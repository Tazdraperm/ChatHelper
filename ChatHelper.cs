using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ExileCore;

namespace ChatHelper
{
    public class ChatHelper : BaseSettingsPlugin<ChatHelperSettings>
    {
        private Socket _socket, _handler;
        private int _lastHash;
        private readonly Stack<byte[]> _chatStack = new Stack<byte[]>();

        public override bool Initialise()
        {
            var ipPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), int.Parse(Settings.Port));
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(ipPoint);
            _socket.Listen(1);
            _socket.BeginAccept(AcceptCallback, null);

            var ui = GameController.Game.IngameState.IngameUi;
            var n = ui.ChatBoxRoot?.MessageBox.ChildCount;
            if (n.HasValue && n > 0)
            {
                var elem = ui.ChatBoxRoot?.MessageBox.Children[(int) n - 1];
                if (elem != null)
                    _lastHash = (elem.Address, elem.Text).GetHashCode();
                DebugWindow.LogMsg($"Last Message Hash: {_lastHash}");
            }

            return base.Initialise();
        }

        public override Job Tick()
        {
            TickLogic();
            return null;
        }

        private void TickLogic()
        {
            if (!Settings.Enable)
                return;

            try
            {
                var ui = GameController.Game.IngameState.IngameUi;
                var n = ui.ChatBoxRoot?.MessageBox.ChildCount;
                if (_handler != null && n.HasValue)
                {
                    var newLastHash = 0;
                    for (var i = n.Value - 1; i >= 0; i--)
                    {
                        var elem = ui.ChatBoxRoot?.MessageBox.Children[(int)i];
                        if (elem == null)
                            continue;

                        var hash = (elem.Address, elem.Text).GetHashCode();
                        if (i == n.Value - 1)
                            newLastHash = hash;

                        if (hash == _lastHash)
                            break;

                        if (Settings.IncomingWhispersOnly && !elem.Text.Contains("@From"))
                            continue;

                        var data = Encoding.UTF8.GetBytes(elem.Text);
                        _chatStack.Push(data);
                    }

                    while (_chatStack.Count > 0)
                    {
                        var data = _chatStack.Pop();
                        _handler.Send(BitConverter.GetBytes((short)data.Length));
                        _handler.Send(data, 0, data.Length, 0);
                    }

                    if (newLastHash != _lastHash)
                    {
                        _lastHash = newLastHash;
                        DebugWindow.LogMsg($"Last Message Hash: {_lastHash}");
                    }
                }
            }
            catch (Exception e)
            {
                DebugWindow.LogError($"ChatHelper.TickLogic: {e.Message}");
            }
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            _handler = _socket.EndAccept(ar);
        }
    }
}

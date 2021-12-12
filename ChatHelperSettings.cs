using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace ChatHelper
{
    public class ChatHelperSettings : ISettings
    {
        public ToggleNode Enable { get; set; }
        public TextNode Port { get; set; } = "8005";
        public ToggleNode IncomingWhispersOnly { get; set; } = new ToggleNode(true);
    }
}

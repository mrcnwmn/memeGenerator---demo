using AppKit;
using Foundation;

namespace MemeGenerator
{
    public partial class AppDelegate : NSApplicationDelegate
    { 
        public AppDelegate()
        {
        }

        [Export("applicationDidFinishLaunching:")]
        public override void DidFinishLaunching(NSNotification notification)
        {
            window.ContentViewController                = new ImageCanvasController();
            window.TitleVisibility                      = NSWindowTitleVisibility.Hidden;
            statusToolbarItem.View                      = ((ImageCanvasController)window.ContentViewController).imageLabel;
            window.MakeKeyAndOrderFront(null);
        }

        [Export("applicationWillTerminate:")]
        public override void WillTerminate(NSNotification notification)
        {
         
        }
    }
}

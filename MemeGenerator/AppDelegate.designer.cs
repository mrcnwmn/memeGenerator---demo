// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace MemeGenerator
{
    [Register("AppDelegate")]
    partial class AppDelegate
	{
		[Outlet]
		AppKit.NSToolbarItem statusToolbarItem { get; set; }

		[Outlet]
		AppKit.NSWindow window { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (window != null) {
				window.Dispose ();
				window = null;
			}

			if (statusToolbarItem != null) {
				statusToolbarItem.Dispose ();
				statusToolbarItem = null;
			}
		}
	}
}

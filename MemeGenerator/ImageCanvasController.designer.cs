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
	partial class ImageCanvasController
	{
		[Outlet]
		public AppKit.NSTextField imageLabel { get; set; }

		[Outlet]
		AppKit.NSTextField placeholderLabel { get; set; }

        [Action("addText:")]
        partial void addText(AppKit.NSToolbarItem sender);

        void ReleaseDesignerOutlets ()
		{
			if (imageLabel != null) {
				imageLabel.Dispose ();
				imageLabel = null;
			}

			if (placeholderLabel != null) {
				placeholderLabel.Dispose ();
				placeholderLabel = null;
			}
		}
	}
}

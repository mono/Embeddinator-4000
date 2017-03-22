using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

namespace UIViewLib
{
    public partial class View : AppKit.NSView
    {
        #region Constructors

        // Called when created from unmanaged code
        public View(IntPtr handle) : base(handle)
        {
            Initialize();
        }

        // Called when created directly from a XIB file
        [Export("initWithCoder:")]
        public View(NSCoder coder) : base(coder)
        {
            Initialize();
        }

        // Shared initialization code
        void Initialize()
        {
        }

        #endregion
    }
}

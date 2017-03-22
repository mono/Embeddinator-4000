using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

namespace UIViewLib
{
    public partial class ViewController : AppKit.NSViewController
    {
        #region Constructors

        // Called when created from unmanaged code
        public ViewController(IntPtr handle) : base(handle)
        {
            Initialize();
        }

        // Called when created directly from a XIB file
        [Export("initWithCoder:")]
        public ViewController(NSCoder coder) : base(coder)
        {
            Initialize();
        }

        // Call to load from the XIB/NIB file
        public ViewController() : base("View", NSBundle.MainBundle)
        {
            Initialize();
        }

        // Shared initialization code
        void Initialize()
        {
        }

        #endregion

        //strongly typed view accessor
        public new View View
        {
            get
            {
                return (View)base.View;
            }
        }
    }
}

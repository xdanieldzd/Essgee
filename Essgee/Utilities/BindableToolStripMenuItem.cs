using System;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics.CodeAnalysis;

namespace Essgee.Utilities
{
	[SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly",
		Justification = "False positive.  IDisposable is inherited via IFunctionality.  See http://stackoverflow.com/questions/8925925/code-analysis-ca1063-fires-when-deriving-from-idisposable-and-providing-implemen for details.")]
	public class BindableToolStripMenuItem : ToolStripMenuItem, IBindableComponent
	{
		public BindableToolStripMenuItem() : base() { }
		public BindableToolStripMenuItem(string text) : base(text) { }
		public BindableToolStripMenuItem(Image image) : base(image) { }
		public BindableToolStripMenuItem(string text, Image image) : base(text, image) { }
		public BindableToolStripMenuItem(string text, Image image, EventHandler onClick) : base(text, image, onClick) { }
		public BindableToolStripMenuItem(string text, Image image, params ToolStripMenuItem[] dropDownItems) : base(text, image, dropDownItems) { }
		public BindableToolStripMenuItem(string text, Image image, EventHandler onClick, Keys shortcutKeys) : base(text, image, onClick, shortcutKeys) { }
		public BindableToolStripMenuItem(string text, Image image, EventHandler onClick, string name) : base(text, image, onClick, name) { }

		BindingContext bindingContext;
		ControlBindingsCollection dataBindings;

		public BindingContext BindingContext
		{
			get
			{
				if (bindingContext == null)
					bindingContext = new BindingContext();
				return bindingContext;
			}
			set
			{
				bindingContext = value;
			}
		}

		public ControlBindingsCollection DataBindings
		{
			get
			{
				if (dataBindings == null)
					dataBindings = new ControlBindingsCollection(this);
				return dataBindings;
			}
		}
	}
}

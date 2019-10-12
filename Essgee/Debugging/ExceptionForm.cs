using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Media;

using Essgee.Exceptions;

namespace Essgee.Debugging
{
	public partial class ExceptionForm : Form
	{
		readonly static Dictionary<Type, (Bitmap, ExceptionResult, string, string)> exceptionInfos = new Dictionary<Type, (Bitmap, ExceptionResult, string, string)>()
		{
			{ typeof(Exception), (SystemIcons.Hand.ToBitmap(), ExceptionResult.ExitApplication, "An unhandled exception has occured!", "Execution cannot continue and the application will be terminated.") },
			{ typeof(CartridgeLoaderException), (SystemIcons.Warning.ToBitmap(), ExceptionResult.Continue, string.Empty, "Failed to load cartridge.") },
			{ typeof(EmulationException), (SystemIcons.Exclamation.ToBitmap(), ExceptionResult.StopEmulation, "An emulation exception has occured!", "Emulation cannot continue and will be terminated.") },
		};

		string messagePrefix, messagePostfix;

		public ExceptionForm(Exception ex)
		{
			InitializeComponent();

			(pbIcon.Image, Tag, messagePrefix, messagePostfix) = GetExceptionInfo(ex);

			var exceptionInfoString = GetExtendedExceptionInfoString(ex);

			var errorBuilder = new StringBuilder();

			var detailsBuilder = new StringBuilder();
			detailsBuilder.AppendLine("--- EXCEPTION ---");
			detailsBuilder.AppendLine();
			detailsBuilder.AppendLine(GetBuildInfoString());
			detailsBuilder.AppendLine();
			detailsBuilder.AppendLine($"Current Date/Time: {DateTime.Now}");

			switch ((ExceptionResult)Tag)
			{
				case ExceptionResult.StopEmulation:
					SystemSounds.Exclamation.Play();

					Text = "Exception";

					errorBuilder.AppendLine(messagePrefix);
					errorBuilder.AppendLine();
					errorBuilder.AppendLine(ex.Message);
					errorBuilder.AppendLine();
					errorBuilder.AppendLine(messagePostfix);

					detailsBuilder.AppendLine(exceptionInfoString);
					break;

				case ExceptionResult.ExitApplication:
					SystemSounds.Hand.Play();

					Text = "Exception";

					errorBuilder.AppendLine(messagePrefix);
					errorBuilder.AppendLine();
					errorBuilder.AppendLine(exceptionInfoString);
					errorBuilder.AppendLine();
					errorBuilder.AppendLine(messagePostfix);

					detailsBuilder.AppendLine(exceptionInfoString);
					detailsBuilder.AppendLine();
					detailsBuilder.AppendLine("Exception occured:");
					detailsBuilder.AppendLine($"{ex.StackTrace}");
					break;
			}

			lblText.MinimumSize = new Size(lblText.Width, 0);

			lblText.Text = errorBuilder.ToString();
			txtDetails.Text = detailsBuilder.ToString();

			txtDetails.VisibleChanged += (s, ev) => { ChangeDetailsButtonText(); };
			txtDetails.Hide();

			ChangeDetailsButtonText();

			btnDetails.Click += (s, ev) =>
			{
				txtDetails.Visible = !txtDetails.Visible;
				OnResize(EventArgs.Empty);
			};
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);

			CenterToScreen();
		}

		private void ChangeDetailsButtonText()
		{
			btnDetails.Text = (txtDetails.Visible ? "Hide &Details..." : "Show &Details...");
		}

		public static (Bitmap, ExceptionResult, string, string) GetExceptionInfo(Exception ex)
		{
			var exceptionType = (exceptionInfos.ContainsKey(ex.GetType()) ? ex.GetType() : typeof(Exception));
			return exceptionInfos[exceptionType];
		}

		private string GetBuildInfoString()
		{
			var version = new Version(Application.ProductVersion);
			var versionMinor = (version.Minor != 0 ? $".{version.Minor}" : string.Empty);

			var buildTimeZone = (TimeZoneInfo)BuildInformation.Properties["BuildTimeZone"];
			var buildDateTime = TimeZoneInfo.ConvertTimeFromUtc((DateTime)BuildInformation.Properties["BuildDate"], buildTimeZone);
			var buildTimeOffset = buildTimeZone.GetUtcOffset(buildDateTime);
			var buildDateTimeString = $"{buildDateTime} (UTC{(buildTimeOffset >= TimeSpan.Zero ? "+" : "-")}{Math.Abs(buildTimeOffset.Hours):D2}:{Math.Abs(buildTimeOffset.Minutes):D2})";

			return $"{Application.ProductName} v{version.Major:D3}{versionMinor} ({Program.BuildName}), built {buildDateTimeString} on {Program.BuildMachineInfo}";
		}

		private string GetExtendedExceptionInfoString(Exception ex)
		{
			var exceptionInfoBuilder = new StringBuilder();
			exceptionInfoBuilder.AppendLine($"Thread: {ex.Data["Thread"] ?? "<unnamed>"}");
			exceptionInfoBuilder.AppendLine($"Function: {ex.TargetSite?.ReflectedType.FullName ?? "<unknown>"}.{ex.TargetSite?.Name ?? "<unknown>"}");
			exceptionInfoBuilder.AppendLine($"Exception: {ex.GetType().Name}");
			exceptionInfoBuilder.AppendLine();
			exceptionInfoBuilder.Append($"Message: {ex.Message}");

			return exceptionInfoBuilder.ToString();
		}
	}
}

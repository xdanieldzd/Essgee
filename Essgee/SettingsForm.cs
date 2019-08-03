﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;

using Essgee.Emulation.Configuration;
using Essgee.Emulation.Machines;

namespace Essgee
{
	public partial class SettingsForm : Form
	{
		readonly static Dictionary<Type, Func<IConfiguration, PropertyInfo, ControlAttribute, (Control[], int[])>> generatorFunctions = new Dictionary<Type, Func<IConfiguration, PropertyInfo, ControlAttribute, (Control[], int[])>>()
		{
			{ typeof(ControlAttribute), GenerateControl },
			{ typeof(DropDownControlAttribute), GenerateDropDownControl },
			{ typeof(TextBoxControlAttribute), GenerateTextBoxControl },
			{ typeof(FileBrowserControlAttribute), GenerateFileBrowserControl },
			{ typeof(CheckBoxControlAttribute), GenerateCheckBoxControl }
		};

		public Dictionary<string, IConfiguration> Configurations { get; private set; }

		public SettingsForm(Dictionary<string, IConfiguration> configs)
		{
			InitializeComponent();

			Configurations = new Dictionary<string, IConfiguration>();
			foreach (var currentConfig in configs.OrderBy(x => x.Value.GetType().GetAttribute<RootPagePriorityAttribute>().Priority))
			{
				var configClone = currentConfig.Value.CloneObject();

				var machineName = currentConfig.Key;
				var machineType = Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(x => typeof(IMachine).IsAssignableFrom(x) && !x.IsInterface && x.Name == currentConfig.Key);
				if (machineType != null)
				{
					var instance = (IMachine)Activator.CreateInstance(machineType);
					machineName = instance.ModelName;
				}
				tcConfigs.TabPages.Add(GenerateMachineTabPage(machineName, configClone));

				Configurations.Add(currentConfig.Key, configClone);
			}
			Height = CalculateMinimumHeight(Height, tcConfigs);

			Load += (s, e) =>
			{
				foreach (TabPage machineTabPage in tcConfigs.TabPages)
					tcConfigs.SelectedTab = machineTabPage;
				tcConfigs.SelectedTab = tcConfigs.TabPages[0];
			};
		}

		private TabPage GenerateMachineTabPage(string machineName, IConfiguration config)
		{
			var machineTabPage = new TabPage()
			{
				Text = machineName,
				UseVisualStyleBackColor = false,
				BackColor = SystemColors.Window,
			};

			var machineConfigTabControl = new TabControl()
			{
				Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
				Appearance = TabAppearance.Normal
			};
			machineTabPage.Controls.Add(machineConfigTabControl);

			var type = config.GetType();
			var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			var categories = new Dictionary<string, List<(Control[] Controls, int[] ColSpans)>>();
			foreach (var property in properties)
			{
				var attrib = property.GetAttribute<ControlAttribute>();
				if (attrib != null && property.CanWrite)
				{
					var attribType = attrib.GetType();
					if (generatorFunctions.ContainsKey(attribType))
					{
						if (!categories.ContainsKey(attrib.Category))
							categories.Add(attrib.Category, new List<(Control[] Controls, int[] ColSpans)>());

						categories[attrib.Category].Add(generatorFunctions[attribType](config, property, attrib));
					}
				}
			}

			foreach (var category in categories)
			{
				if (category.Value.Count == 0) continue;

				var categoryData = category.Value;

				var categoryTabPage = new TabPage()
				{
					Text = category.Key,
					UseVisualStyleBackColor = false,
					BackColor = SystemColors.Window
				};

				var tableLayout = new TableLayoutPanel()
				{
					Dock = DockStyle.Fill,
					AutoSize = true,
					AutoSizeMode = AutoSizeMode.GrowAndShrink,
					ColumnCount = 3,
					RowCount = categoryData.Count
				};

				for (int c = 0; c < tableLayout.ColumnCount; c++)
				{
					if (c == 0)
						tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120.0f));
					else if (c == tableLayout.ColumnCount - 1)
						tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30.0f));
					else
						tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
				}

				for (int c = 0; c < tableLayout.ColumnCount; c++)
				{
					for (int r = 0; r < tableLayout.RowCount; r++)
					{
						var (currentControls, currentColSpans) = categoryData[r];

						tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

						if (c < currentControls.Length)
						{
							currentControls[c].TabIndex = ((r * 3) + c);
							tableLayout.Controls.Add(currentControls[c], c, r);
							tableLayout.SetColumnSpan(currentControls[c], currentColSpans[c]);
						}
					}
				}

				tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f));
				tableLayout.RowCount++;

				categoryTabPage.Controls.Add(tableLayout);
				categoryTabPage.Padding = new Padding(0, 3, 0, 3);

				machineConfigTabControl.TabPages.Add(categoryTabPage);
			}

			machineTabPage.Padding = new Padding(3);
			machineTabPage.Height = CalculateMinimumHeight(machineTabPage.Height, machineConfigTabControl);

			return machineTabPage;
		}

		private int CalculateMinimumHeight(int parentHeight, TabControl tabControl)
		{
			var maxTabPageHeight = tabControl.TabPages.Cast<TabPage>().Max(x => (x as Control).PreferredSize.Height);
			var tabControlDiff = (parentHeight - tabControl.Height);
			var tabPageDiff = (tabControl.Height - tabControl.DisplayRectangle.Height);

			return (tabControlDiff + tabPageDiff + maxTabPageHeight);
		}

		private static Size GetDefaultSize(Type controlType)
		{
			var property = controlType.GetProperty("DefaultSize", BindingFlags.NonPublic | BindingFlags.Instance);
			var instance = Activator.CreateInstance(controlType);
			return (Size)property.GetValue(instance, null);
		}

		private static (Control[], int[]) GenerateControl(IConfiguration configuration, PropertyInfo property, ControlAttribute attribute)
		{
			var labelControl = GenerateBasicLabelControl(attribute);

			var targetHeight = GetDefaultSize(typeof(ComboBox)).Height;
			var padding = (targetHeight - labelControl.PreferredHeight) / 2;
			if (padding > 0)
				labelControl.Padding = new Padding(labelControl.Padding.Left, padding, labelControl.Padding.Right, padding);

			return (new Control[] { labelControl }, new int[] { 3 });
		}

		private static (Control[], int[]) GenerateDropDownControl(IConfiguration configuration, PropertyInfo property, ControlAttribute attribute)
		{
			var labelControl = GenerateBasicLabelControl(attribute);

			var dropDownAttrib = (attribute as DropDownControlAttribute);
			var comboBoxControl = new ComboBox()
			{
				Dock = DockStyle.Fill,
				DropDownStyle = ComboBoxStyle.DropDownList,
				DataSource = dropDownAttrib.Values.ToList(),
				DisplayMember = "Key",
				ValueMember = "Value"
			};
			comboBoxControl.DataBindings.Add(nameof(comboBoxControl.SelectedValue), configuration, property.Name, false, DataSourceUpdateMode.OnPropertyChanged);

			return (new Control[] { labelControl, comboBoxControl }, new int[] { 1, 2 });
		}

		private static (Control[], int[]) GenerateTextBoxControl(IConfiguration configuration, PropertyInfo property, ControlAttribute attribute)
		{
			var labelControl = GenerateBasicLabelControl(attribute);
			var textBoxControl = new TextBox() { Dock = DockStyle.Fill };
			textBoxControl.DataBindings.Add(nameof(textBoxControl.Text), configuration, property.Name, false, DataSourceUpdateMode.OnPropertyChanged);

			return (new Control[] { labelControl, textBoxControl }, new int[] { 1, 2 });
		}

		private static (Control[], int[]) GenerateFileBrowserControl(IConfiguration configuration, PropertyInfo property, ControlAttribute attribute)
		{
			var labelControl = GenerateBasicLabelControl(attribute);

			var fileBrowserAttrib = (attribute as FileBrowserControlAttribute);
			var textBoxControl = new TextBox()
			{
				Dock = DockStyle.Fill,
				ReadOnly = true,
				BackColor = SystemColors.Window
			};
			textBoxControl.DataBindings.Add(nameof(textBoxControl.Text), configuration, property.Name, false, DataSourceUpdateMode.OnPropertyChanged);

			var browseButtonControl = new Button()
			{
				Dock = DockStyle.Fill,
				Text = "...",
				Height = textBoxControl.Height,
				UseVisualStyleBackColor = true,
				Tag = (textBoxControl, configuration, property)
			};
			browseButtonControl.Click += (s, e) =>
			{
				var (textBox, config, prop) = (ValueTuple<TextBox, IConfiguration, PropertyInfo>)(s as Control).Tag;

				var openFileDialog = new OpenFileDialog() { Filter = fileBrowserAttrib.Filter };
				if (openFileDialog.ShowDialog() == DialogResult.OK)
				{
					prop.SetValue(config, openFileDialog.FileName);
					textBox.DataBindings[nameof(textBox.Text)].ReadValue();
				}
			};

			return (new Control[] { labelControl, textBoxControl, browseButtonControl }, new int[] { 1, 1, 1 });
		}

		private static (Control[], int[]) GenerateCheckBoxControl(IConfiguration configuration, PropertyInfo property, ControlAttribute attribute)
		{
			var checkBoxAttrib = (attribute as CheckBoxControlAttribute);
			var checkBoxControl = new CheckBox()
			{
				Dock = DockStyle.Fill,
				AutoSize = true,
				Padding = new Padding(3, 0, 0, 0),
				Text = checkBoxAttrib.Label
			};
			checkBoxControl.DataBindings.Add(nameof(checkBoxControl.Checked), configuration, property.Name, false, DataSourceUpdateMode.OnPropertyChanged);

			return (new Control[] { checkBoxControl }, new int[] { 3 });
		}

		private static Label GenerateBasicLabelControl(ControlAttribute attribute)
		{
			return new Label()
			{
				Text = attribute.Label,
				AutoSize = true,
				TextAlign = ContentAlignment.MiddleLeft,
				Dock = DockStyle.Fill
			};
		}
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class RootPagePriorityAttribute : Attribute
	{
		public int Priority { get; set; }

		public RootPagePriorityAttribute(int priority)
		{
			Priority = priority;
		}
	}

	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public abstract class ControlAttribute : Attribute
	{
		public string Category { get; set; }
		public string Label { get; set; }

		public ControlAttribute(string category, string label)
		{
			Category = category;
			Label = label;
		}
	}

	public class DropDownControlAttribute : ControlAttribute
	{
		// TODO: does this even help??
		static readonly Dictionary<string, List<KeyValuePair<string, object>>> cache = new Dictionary<string, List<KeyValuePair<string, object>>>();

		public List<KeyValuePair<string, object>> Values { get; set; }

		public DropDownControlAttribute(string category, string label, Dictionary<string, object> dictionary) : base(category, label)
		{
			var dictHash = dictionary.GetHashCode().ToString();
			if (cache.ContainsKey(dictHash))
				Values = cache[dictHash];
			else
				cache[dictHash] = Values = dictionary.ToList();
		}

		public DropDownControlAttribute(string category, string label, Type valueType) : base(category, label)
		{
			if (cache.ContainsKey(valueType.FullName))
				Values = cache[valueType.FullName];
			else
			{
				var dict = new Dictionary<string, object>();
				foreach (var value in Enum.GetValues(valueType))
				{
					var ignore = value.GetType().GetField(value.ToString())?.GetAttribute<ValueIgnoredAttribute>()?.IsIgnored;
					if (ignore ?? false) continue;

					var key = value.GetType().GetField(value.ToString())?.GetAttribute<DescriptionAttribute>()?.Description ?? value.ToString();
					if (!dict.ContainsKey(key)) dict.Add(key, value);
				}
				cache[valueType.FullName] = Values = dict.ToList();
			}
		}
	}

	public class TextBoxControlAttribute : ControlAttribute
	{
		public TextBoxControlAttribute(string category, string label) : base(category, label) { }
	}

	public class FileBrowserControlAttribute : ControlAttribute
	{
		public string Filter { get; set; }

		public FileBrowserControlAttribute(string category, string label, string filter) : base(category, label)
		{
			Filter = filter;
		}
	}

	public class CheckBoxControlAttribute : ControlAttribute
	{
		public CheckBoxControlAttribute(string category, string label) : base(category, label) { }
	}

	public class ValueIgnoredAttribute : Attribute
	{
		public bool IsIgnored { get; set; }

		public ValueIgnoredAttribute() : base()
		{
			IsIgnored = false;
		}

		public ValueIgnoredAttribute(bool isIgnored)
		{
			IsIgnored = isIgnored;
		}
	}
}

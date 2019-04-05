﻿using System;
using System.CodeDom.Compiler;
using System.IO;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace Terraria.ModLoader.UI
{
	internal class UICreateMod : UIState
	{
		private UITextPanel<string> _messagePanel;
		private UIFocusInputTextField _modAuthor;
		private UIFocusInputTextField _modDisplayName;
		private UIFocusInputTextField _modName;

		public override void OnActivate() {
			base.OnActivate();
			_modName.SetText("");
			_modDisplayName.SetText("");
			_modAuthor.SetText("");
			_messagePanel.SetText("");
		}

		public override void OnInitialize() {
			var uIElement = new UIElement {
				Width = { Percent = 0.8f },
				MaxWidth = UICommon.MAX_PANEL_WIDTH,
				Top = { Pixels = 220 },
				Height = { Pixels = -220, Percent = 1f },
				HAlign = 0.5f
			};
			Append(uIElement);

			var mainPanel = new UIPanel {
				Width = { Percent = 1f },
				Height = { Pixels = -110, Percent = 1f },
				BackgroundColor = UICommon.MAIN_PANEL_BG_COLOR,
				PaddingTop = 0f
			};
			uIElement.Append(mainPanel);

			var uITextPanel = new UITextPanel<string>(Language.GetTextValue("tModLoader.MSCreateMod"), 0.8f, true) {
				HAlign = 0.5f,
				Top = { Pixels = -35 },
				BackgroundColor = UICommon.UI_BLUE_COLOR
			}.WithPadding(15);
			uIElement.Append(uITextPanel);

			_messagePanel = new UITextPanel<string>(Language.GetTextValue("")) {
				Width = { Percent = 1f },
				Height = { Pixels = 25 },
				VAlign = 1f,
				Top = { Pixels = -20 }
			};
			uIElement.Append(_messagePanel);

			var buttonBack = new UITextPanel<string>(Language.GetTextValue("UI.Back")) {
				Width = { Pixels = -10, Percent = 0.5f },
				Height = { Pixels = 25 },
				VAlign = 1f,
				Top = { Pixels = -65 }
			}.WithFadedMouseOver();
			buttonBack.OnClick += OnClickBack;
			uIElement.Append(buttonBack);

			var buttonCreate = new UITextPanel<string>(Language.GetTextValue("LegacyMenu.28")); // Create
			buttonCreate.CopyStyle(buttonBack);
			buttonCreate.HAlign = 1f;
			buttonCreate.WithFadedMouseOver();
			buttonCreate.OnClick += OnClickCreate;
			uIElement.Append(buttonCreate);

			float top = 16;
			_modName = CreateAndAppendTextInputWithLabel("ModName (no spaces)", "Type here");
			_modName.OnTextChange += (a, b) => { _modName.SetText(_modName.currentString.Replace(" ", "")); };
			_modDisplayName = CreateAndAppendTextInputWithLabel("Mod DisplayName", "Type here");
			_modAuthor = CreateAndAppendTextInputWithLabel("Mod Author", "Type here");
			// TODO: OnTab
			// TODO: Starting Item checkbox

			UIFocusInputTextField CreateAndAppendTextInputWithLabel(string label, string hint) {
				var panel = new UIPanel();
				panel.SetPadding(0);
				panel.Width.Set(0, 1f);
				panel.Height.Set(40, 0f);
				panel.Top.Set(top, 0f);
				top += 46;

				var modNameText = new UIText(label) {
					Left = { Pixels = 10 },
					Top = { Pixels = 10 }
				};

				panel.Append(modNameText);

				var textBoxBackground = new UIPanel();
				textBoxBackground.SetPadding(0);
				textBoxBackground.Top.Set(6f, 0f);
				textBoxBackground.Left.Set(0, .5f);
				textBoxBackground.Width.Set(0, .5f);
				textBoxBackground.Height.Set(30, 0f);
				panel.Append(textBoxBackground);

				var uIInputTextField = new UIFocusInputTextField(hint);
				uIInputTextField.Top.Set(5, 0f);
				uIInputTextField.Left.Set(10, 0f);
				uIInputTextField.Width.Set(-20, 1f);
				uIInputTextField.Height.Set(20, 0);
				textBoxBackground.Append(uIInputTextField);

				mainPanel.Append(panel);

				return uIInputTextField;
			}
		}

		private void OnClickBack(UIMouseEvent evt, UIElement listeningElement) {
			Main.PlaySound(SoundID.MenuClose);
			Main.menuMode = Interface.modSourcesID;
		}

		private void OnClickCreate(UIMouseEvent evt, UIElement listeningElement) {
			string modNameTrimmed = _modName.currentString.Trim();
			string sourceFolder = ModCompile.ModSourcePath + Path.DirectorySeparatorChar + modNameTrimmed;
			var provider = CodeDomProvider.CreateProvider("C#");
			if (Directory.Exists(sourceFolder))
				_messagePanel.SetText("Folder already exists");
			else if (!provider.IsValidIdentifier(modNameTrimmed))
				_messagePanel.SetText("ModName is invalid C# identifier. Remove spaces.");
			else if (string.IsNullOrWhiteSpace(_modDisplayName.currentString))
				_messagePanel.SetText("DisplayName can't be empty");
			else if (string.IsNullOrWhiteSpace(_modAuthor.currentString))
				_messagePanel.SetText("Author can't be empty");
			else if (string.IsNullOrWhiteSpace(_modAuthor.currentString))
				_messagePanel.SetText("Author can't be empty");
			else {
				Main.PlaySound(SoundID.MenuOpen);
				Main.menuMode = Interface.modSourcesID;
				Directory.CreateDirectory(sourceFolder);

				// TODO: Simple ModItem and PNG, verbatim line endings, localization.
				// TODO move the contents of files being written to resource files
				File.WriteAllText(Path.Combine(sourceFolder, "build.txt"), $"displayName = {_modDisplayName.currentString}{Environment.NewLine}author = {_modAuthor.currentString}{Environment.NewLine}version = 0.1");
				File.WriteAllText(Path.Combine(sourceFolder, "description.txt"), $"{_modDisplayName.currentString} is a pretty cool mod, it does...this. Modify this file with a description of your mod.");
				File.WriteAllText(Path.Combine(sourceFolder, $"{modNameTrimmed}.cs"), $@"using Terraria.ModLoader;

namespace {modNameTrimmed}
{{
	class {modNameTrimmed} : Mod
	{{
		public {modNameTrimmed}()
		{{
		}}
	}}
}}");
				File.WriteAllText(Path.Combine(sourceFolder, $"{modNameTrimmed}.csproj"), $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project Sdk=""Microsoft.NET.Sdk"">
  <Import Project=""..\..\references\tModLoader.targets"" />
  <PropertyGroup>
    <AssemblyName>{modNameTrimmed}</AssemblyName>
    <TargetFramework>net45</TargetFramework>
    <PlatformTarget>x86</PlatformTarget>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)'=='Debug'"">
    <DefineConstants>$(DefineConstants);DEBUG</DefineConstants>
  </PropertyGroup>
  <Target Name=""BuildMod"" AfterTargets=""Build"">
    <Exec Command=""&quot;$(tMLServerPath)&quot; -build $(ProjectDir) -eac $(TargetPath)"" />
  </Target>
</Project>");
				string propertiesFolder = sourceFolder + Path.DirectorySeparatorChar + "Properties";
				Directory.CreateDirectory(propertiesFolder);
				File.WriteAllText(Path.Combine(propertiesFolder, "launchSettings.json"), @"{
  ""profiles"": {
    ""Terraria"": {
      ""commandName"": ""Executable"",
      ""executablePath"": ""$(tMLPath)"",
      ""workingDirectory"": ""$(TerrariaSteamPath)""
    },
    ""TerrariaServer"": {
      ""commandName"": ""Executable"",
      ""executablePath"": ""$(tMLServerPath)"",
      ""workingDirectory"": ""$(TerrariaSteamPath)""
    }
  }
}");
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;
using Terraria.Graphics;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace Terraria.ModLoader.UI
{
	//TODO common 'Item' code
	internal class UIModSourceItem : UIPanel
	{
		private readonly LocalMod _builtMod;
		private readonly Texture2D _dividerTexture;
		private readonly string _mod;
		private readonly UIText _modName;

		public UIModSourceItem(string mod, LocalMod builtMod) {
			_mod = mod;

			BorderColor = new Color(89, 116, 213) * 0.7f;
			_dividerTexture = TextureManager.Load("Images/UI/Divider");
			Height.Pixels = 90;
			Width.Percent = 1f;
			SetPadding(6f);

			string addendum = Path.GetFileName(mod).Contains(" ") ? $"  [c/FF0000:{Language.GetTextValue("tModLoader.MSModSourcesCantHaveSpaces")}]" : "";
			_modName = new UIText(Path.GetFileName(mod) + addendum) {
				Left = { Pixels = 10 },
				Top = { Pixels = 5 }
			};
			Append(_modName);

			var button = new UIScalingTextPanel<string>(Language.GetTextValue("tModLoader.MSBuild")) {
				Width = { Pixels = 100 },
				Height = { Pixels = 36 },
				Left = { Pixels = 10 },
				Top = { Pixels = 40 }
			}.WithFadedMouseOver();
			button.PaddingTop -= 2f;
			button.PaddingBottom -= 2f;
			button.OnClick += BuildMod;
			Append(button);

			var button2 = new UIScalingTextPanel<string>(Language.GetTextValue("tModLoader.MSBuildReload"));
			button2.CopyStyle(button);
			button2.Width.Pixels = 200;
			button2.Left.Pixels = 150;
			button2.WithFadedMouseOver();
			button2.OnClick += BuildAndReload;
			Append(button2);

			_builtMod = builtMod;
			if (builtMod != null) {
				var button3 = new UIScalingTextPanel<string>(Language.GetTextValue("tModLoader.MSPublish"));
				button3.CopyStyle(button2);
				button3.Width.Pixels = 100;
				button3.Left.Pixels = 390;
				button3.WithFadedMouseOver();
				button3.OnClick += Publish;
				Append(button3);
			}

			OnDoubleClick += BuildAndReload;
		}

		public override int CompareTo(object obj) {
			UIModSourceItem uIModSourceItem = obj as UIModSourceItem;
			if (uIModSourceItem == null) {
				return base.CompareTo(obj);
			}

			if (uIModSourceItem._builtMod == null && _builtMod == null)
				return string.Compare(_modName.Text, uIModSourceItem._modName.Text, StringComparison.Ordinal);
			if (uIModSourceItem._builtMod == null)
				return -1;
			if (_builtMod == null)
				return 1;
			return uIModSourceItem._builtMod.lastModified.CompareTo(_builtMod.lastModified);
		}

		public override void MouseOut(UIMouseEvent evt) {
			base.MouseOut(evt);
			BackgroundColor = new Color(63, 82, 151) * 0.7f;
			BorderColor = new Color(89, 116, 213) * 0.7f;
		}

		public override void MouseOver(UIMouseEvent evt) {
			base.MouseOver(evt);
			BackgroundColor = UICommon.UI_BLUE_COLOR;
			BorderColor = new Color(89, 116, 213);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			base.DrawSelf(spriteBatch);
			CalculatedStyle innerDimensions = GetInnerDimensions();
			Vector2 drawPos = new Vector2(innerDimensions.X + 5f, innerDimensions.Y + 30f);
			spriteBatch.Draw(_dividerTexture, drawPos, null, Color.White,
							 0f, Vector2.Zero, new Vector2((innerDimensions.Width - 10f) / 8f, 1f), SpriteEffects.None,
							 0f);
		}

		private void BuildAndReload(UIMouseEvent evt, UIElement listeningElement) {
			Main.PlaySound(10, -1, -1, 1);
			ModLoader.modToBuild = _mod;
			ModLoader.reloadAfterBuild = true;
			ModLoader.buildAll = false;
			Main.menuMode = Interface.buildModID;
		}

		private void BuildMod(UIMouseEvent evt, UIElement listeningElement) {
			Main.PlaySound(10, -1, -1, 1);
			ModLoader.modToBuild = _mod;
			ModLoader.reloadAfterBuild = false;
			ModLoader.buildAll = false;
			Main.menuMode = Interface.buildModID;
		}

		private void Publish(UIMouseEvent evt, UIElement listeningElement) {
			if (ModLoader.modBrowserPassphrase == "") {
				Main.menuMode = Interface.enterPassphraseMenuID;
				Interface.enterPassphraseMenu.SetGotoMenu(Interface.modSourcesID);
				return;
			}

			Main.PlaySound(10);
			try {
				var modFile = _builtMod.modFile;
				var bp = _builtMod.properties;

				var files = new List<UploadFile>();
				files.Add(new UploadFile {
					Name = "file",
					Filename = Path.GetFileName(modFile.path),
					//    ContentType = "text/plain",
					Content = File.ReadAllBytes(modFile.path)
				});
				if (modFile.HasFile("icon.png")) {
					files.Add(new UploadFile {
						Name = "iconfile",
						Filename = "icon.png",
						Content = modFile.GetBytes("icon.png")
					});
				}

				if (bp.beta)
					throw new WebException(Language.GetTextValue("tModLoader.BetaModCantPublishError"));
				if (bp.buildVersion != modFile.tModLoaderVersion)
					throw new WebException(Language.GetTextValue("OutdatedModCantPublishError.BetaModCantPublishError"));

				var values = new NameValueCollection {
					{ "displayname", bp.displayName },
					{ "name", modFile.name },
					{ "version", "v" + bp.version },
					{ "author", bp.author },
					{ "homepage", bp.homepage },
					{ "description", bp.description },
					{ "steamid64", ModLoader.SteamID64 },
					{ "modloaderversion", "tModLoader v" + modFile.tModLoaderVersion },
					{ "passphrase", ModLoader.modBrowserPassphrase },
					{ "modreferences", string.Join(", ", bp.modReferences.Select(x => x.mod)) },
					{ "modside", bp.side.ToFriendlyString() }
				};
				if (values["steamid64"].Length != 17)
					throw new WebException($"The steamid64 '{values["steamid64"]}' is invalid, verify that you are logged into Steam and don't have a pirated copy of Terraria.");
				if (string.IsNullOrEmpty(values["author"]))
					throw new WebException("You need to specify an author in build.txt");
				ServicePointManager.Expect100Continue = false;
				string url = "http://javid.ddns.net/tModLoader/publishmod.php";
				using (PatientWebClient client = new PatientWebClient()) {
					ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, policyErrors) => true;
					Interface.uploadMod.SetDownloading(modFile.name);
					Interface.uploadMod.SetCancel(() => {
						Main.menuMode = Interface.modSourcesID;
						client.CancelAsync();
					});
					client.UploadProgressChanged += (s, e) => Interface.uploadMod.SetProgress(e);
					client.UploadDataCompleted += (s, e) => PublishUploadDataComplete(s, e, modFile);

					var boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x", NumberFormatInfo.InvariantInfo);
					client.Headers["Content-Type"] = "multipart/form-data; boundary=" + boundary;
					//boundary = "--" + boundary;
					byte[] data = UploadFile.GetUploadFilesRequestData(files, values);
					client.UploadDataAsync(new Uri(url), data);
				}

				Main.menuMode = Interface.uploadModID;
			}
			catch (WebException e) {
				UIModBrowser.LogModBrowserException(e);
			}
		}

		private void PublishUploadDataComplete(object s, UploadDataCompletedEventArgs e, TmodFile theTModFile) {
			if (e.Error != null) {
				if (e.Cancelled) {
					Main.menuMode = Interface.modSourcesID;
					return;
				}

				UIModBrowser.LogModBrowserException(e.Error);
				return;
			}

			var result = e.Result;
			int responseLength = result.Length;
			if (result.Length > 256 && result[result.Length - 256 - 1] == '~') {
				using (var fileStream = File.Open(theTModFile.path, FileMode.Open, FileAccess.ReadWrite))
				using (var fileReader = new BinaryReader(fileStream))
				using (var fileWriter = new BinaryWriter(fileStream)) {
					fileReader.ReadBytes(4); // "TMOD"
					fileReader.ReadString(); // ModLoader.version.ToString()
					fileReader.ReadBytes(20); // hash
					if (fileStream.Length - fileStream.Position > 256) // Extrememly basic check in case ReadString errors?
						fileWriter.Write(result, result.Length - 256, 256);
				}

				responseLength -= 257;
			}

			string response = Encoding.UTF8.GetString(result, 0, responseLength);
			UIModBrowser.LogModPublishInfo(response);
		}

		private class PatientWebClient : WebClient
		{
			protected override WebRequest GetWebRequest(Uri uri) {
				HttpWebRequest w = (HttpWebRequest)base.GetWebRequest(uri);
				w.Timeout = Timeout.Infinite;
				w.AllowWriteStreamBuffering = false; // Should use less ram.
				return w;
			}
		}
	}
}

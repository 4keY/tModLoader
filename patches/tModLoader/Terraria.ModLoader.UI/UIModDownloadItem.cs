using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Text;
using Terraria.GameContent.UI.Elements;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace Terraria.ModLoader.UI
{
	internal class UIModDownloadItem : UIPanel
	{
		public string mod;
		public string displayName;
		public string version;
		public string author;
		public string modIconUrl;
		public string download;
		public string timeStamp;
		public string modreferences;
		public ModSide modside;
		public int downloads;
		public int hot;
		public bool update;
		public bool updateIsDowngrade;
		public LocalMod installed;

		private UIImage _modIcon;
		private bool _modIconWanted;
		private bool _modIconRequested;
		private bool _modIconReady;
		private bool _modIconAppended; // mod icon was ready, and is now appended
		private bool HasModIcon => modIconUrl != null;
		private float ModIconAdjust => _modIconAppended ? 85f : 0f;
		private readonly Texture2D _dividerTexture;
		private readonly Texture2D _innerPanelTexture;
		private readonly UIText _modName;
		private readonly UIScalingTextPanel<string> _updateButton;
		private readonly UIScalingTextPanel<string> _moreInfoButton;

		public UIModDownloadItem(string displayName, string name, string version, string author, string modreferences, ModSide modside, string modIconUrl, string download, int downloads, int hot, string timeStamp, bool update, bool updateIsDowngrade, LocalMod installed) {
			this.displayName = displayName;
			this.mod = name;
			this.version = version;
			this.author = author;
			this.modreferences = modreferences;
			this.modside = modside;
			this.modIconUrl = modIconUrl;
			this.download = download;
			this.downloads = downloads;
			this.hot = hot;
			this.timeStamp = timeStamp;
			this.update = update;
			this.updateIsDowngrade = updateIsDowngrade;
			this.installed = installed;

			BorderColor = new Color(89, 116, 213) * 0.7f;
			_dividerTexture = TextureManager.Load("Images/UI/Divider");
			_innerPanelTexture = TextureManager.Load("Images/UI/InnerPanelBackground");
			Height.Pixels = 90;
			Width.Percent = 1f;
			SetPadding(6f);

			float left = HasModIcon ? 85f : 0f;
			string text = displayName + " " + version;
			_modName = new UIText(text) {
				Left = new StyleDimension(left + 5, 0f),
				Top = { Pixels = 5 }
			};
			Append(_modName);

			_moreInfoButton = new UIScalingTextPanel<string>(Language.GetTextValue("tModLoader.ModsMoreInfo")) {
				Width = { Pixels = 100 },
				Height = { Pixels = 36 },
				Left = { Pixels = left },
				Top = { Pixels = 40 }
			}.WithFadedMouseOver();
			_moreInfoButton.PaddingTop -= 2f;
			_moreInfoButton.PaddingBottom -= 2f;
			_moreInfoButton.OnClick += RequestMoreinfo;
			Append(_moreInfoButton);

			if (update || installed == null) {
				_updateButton = new UIScalingTextPanel<string>(this.update ? (updateIsDowngrade ? Language.GetTextValue("tModLoader.MBDowngrade") : Language.GetTextValue("tModLoader.MBUpdate")) : Language.GetTextValue("tModLoader.MBDownload"), 1f,
															   false);
				_updateButton.CopyStyle(_moreInfoButton);
				_updateButton.Width.Pixels = HasModIcon ? 120 : 200;
				_updateButton.Left.Pixels = _moreInfoButton.Width.Pixels + _moreInfoButton.Left.Pixels + 5f;
				_updateButton.WithFadedMouseOver();
				_updateButton.OnClick += DownloadMod;
				Append(_updateButton);
			}

			if (modreferences.Length > 0) {
				var icon = Texture2D.FromStream(Main.instance.GraphicsDevice,
												Assembly.GetExecutingAssembly().GetManifestResourceStream("Terraria.ModLoader.UI.ButtonExclamation.png"));
				var modReferenceIcon = new UIHoverImage(icon, Language.GetTextValue("tModLoader.MBClickToViewDependencyMods", string.Join("\n", modreferences.Split(',').Select(x => x.Trim())))) {
					Left = { Pixels = -149, Percent = 1f },
					Top = { Pixels = 48 }
				};
				modReferenceIcon.OnClick += (s, e) => {
					var modListItem = (UIModDownloadItem)e.Parent;
					Interface.modBrowser.SpecialModPackFilter = modListItem.modreferences.Split(',').Select(x => x.Trim()).ToList();
					Interface.modBrowser.SpecialModPackFilterTitle = Language.GetTextValue("tModLoader.MBFilterDependencies"); // Toolong of \n" + modListItem.modName.Text;
					Interface.modBrowser.filterTextBox.Text = "";
					Interface.modBrowser.updateNeeded = true;
					Main.PlaySound(SoundID.MenuOpen);
				};
				Append(modReferenceIcon);
			}

			OnDoubleClick += RequestMoreinfo;
		}

		public override int CompareTo(object obj) {
			var item = obj as UIModDownloadItem;
			switch (Interface.modBrowser.sortMode) {
				default:
					return base.CompareTo(obj);
				case ModBrowserSortMode.DisplayNameAtoZ:
					return string.Compare(this.displayName, item?.displayName, StringComparison.Ordinal);
				case ModBrowserSortMode.DisplayNameZtoA:
					return -1 * string.Compare(this.displayName, item?.displayName, StringComparison.Ordinal);
				case ModBrowserSortMode.DownloadsAscending:
					return this.downloads.CompareTo(item?.downloads);
				case ModBrowserSortMode.DownloadsDescending:
					return -1 * this.downloads.CompareTo(item?.downloads);
				case ModBrowserSortMode.RecentlyUpdated:
					return -1 * string.Compare(this.timeStamp, item?.timeStamp, StringComparison.Ordinal);
				case ModBrowserSortMode.Hot:
					return -1 * this.hot.CompareTo(item?.hot);
			}
		}

		public bool PassFilters() {
			if (Interface.modBrowser.SpecialModPackFilter != null && !Interface.modBrowser.SpecialModPackFilter.Contains(mod)) {
				return false;
			}

			if (Interface.modBrowser.filter.Length > 0) {
				if (Interface.modBrowser.searchFilterMode == SearchFilter.Author) {
					if (author.IndexOf(Interface.modBrowser.filter, StringComparison.OrdinalIgnoreCase) == -1) {
						return false;
					}
				}
				else {
					if (displayName.IndexOf(Interface.modBrowser.filter, StringComparison.OrdinalIgnoreCase) == -1 && mod.IndexOf(Interface.modBrowser.filter, StringComparison.OrdinalIgnoreCase) == -1) {
						return false;
					}
				}
			}

			if (Interface.modBrowser.modSideFilterMode != ModSideFilter.All) {
				if ((int)modside != (int)Interface.modBrowser.modSideFilterMode - 1)
					return false;
			}

			switch (Interface.modBrowser.updateFilterMode) {
				default:
				case UpdateFilter.All:
					return true;
				case UpdateFilter.Available:
					return update || installed == null;
				case UpdateFilter.UpdateOnly:
					return update;
			}
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			base.DrawSelf(spriteBatch);

			if (HasModIcon && !_modIconWanted) {
				_modIconWanted = true;
			}

			CalculatedStyle innerDimensions = GetInnerDimensions();
			//draw divider
			Vector2 drawPos = new Vector2(innerDimensions.X + 5f + ModIconAdjust, innerDimensions.Y + 30f);
			spriteBatch.Draw(this._dividerTexture, drawPos, null, Color.White,
							 0f, Vector2.Zero, new Vector2((innerDimensions.Width - 10f - ModIconAdjust) / 8f, 1f), SpriteEffects.None,
							 0f);
			// change pos for button
			const int baseWidth = 125; // something like 1 days ago is ~110px, XX minutes ago is ~120 px (longest)
			drawPos = new Vector2(innerDimensions.X + innerDimensions.Width - baseWidth, innerDimensions.Y + 45);
			DrawPanel(spriteBatch, drawPos, baseWidth);
			DrawTimeText(spriteBatch, drawPos + new Vector2(0f, 2f), baseWidth); // x offset (padding) to do in method
		}

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);
			if (_modIconWanted && !_modIconRequested) {
				_modIconRequested = true;
				using (var client = new WebClient()) {
					client.DownloadDataCompleted += IconDownloadComplete;
					client.DownloadDataAsync(new Uri(modIconUrl));
				}
			}

			if (_modIconReady) {
				_modIconReady = false;
				_modIconAppended = true;
				Append(_modIcon);
			}
		}

		private void IconDownloadComplete(object sender, DownloadDataCompletedEventArgs e) {
			try {
				byte[] data = e.Result;
				using (var buffer = new MemoryStream(data)) {
					var iconTexture = Texture2D.FromStream(Main.instance.GraphicsDevice, buffer);
					_modIcon = new UIImage(iconTexture) {
						Left = { Percent = 0f },
						Top = { Percent = 0f }
					};
					_modIconReady = true; // We'd like to avoid collection modified exceptions
					_modIconWanted = false; // We got the icon, no longer wanted
				}
			}
			catch (Exception) {
				// country- wide imgur blocks, cannot load icon
				_modIconReady = false;
				_modIconWanted = false;
				_modName.Left.Set(5f, 0f);
				_moreInfoButton.Left.Set(0f, 0f);
				_updateButton.Left.Set(_moreInfoButton.Width.Pixels + 5f, 0f);
			}
		}

		protected override void DrawChildren(SpriteBatch spriteBatch) {
			base.DrawChildren(spriteBatch);

			// show authors on mod title hover, after everything else
			// main.hoverItemName isn't drawn in UI
			if (_modName.IsMouseHovering) {
				string text = Language.GetTextValue("tModLoader.ModsByline", author);
				UICommon.DrawHoverStringInBounds(spriteBatch, text);
			}
		}

		private void DrawPanel(SpriteBatch spriteBatch, Vector2 position, float width) {
			spriteBatch.Draw(_innerPanelTexture, position, new Rectangle?(new Rectangle(0, 0, 8, _innerPanelTexture.Height)), Color.White);
			spriteBatch.Draw(_innerPanelTexture, new Vector2(position.X + 8f, position.Y), new Rectangle?(new Rectangle(8, 0, 8, _innerPanelTexture.Height)), Color.White,
							 0f, Vector2.Zero, new Vector2((width - 16f) / 8f, 1f), SpriteEffects.None,
							 0f);
			spriteBatch.Draw(_innerPanelTexture, new Vector2(position.X + width - 8f, position.Y), new Rectangle?(new Rectangle(16, 0, 8, _innerPanelTexture.Height)), Color.White);
		}

		private void DrawTimeText(SpriteBatch spriteBatch, Vector2 drawPos, int baseWidth) {
			if (timeStamp == "0000-00-00 00:00:00") {
				return;
			}

			try {
				var myDateTime = DateTime.Parse(timeStamp); // parse date
				string text = TimeHelper.HumanTimeSpanString(myDateTime); // get time text
				int textWidth = (int)Main.fontMouseText.MeasureString(text).X; // measure text width
				int diffWidth = baseWidth - textWidth; // get difference
				drawPos.X += diffWidth * 0.5f; // add difference as padding
				Utils.DrawBorderString(spriteBatch, text, drawPos, Color.White,
									   1f, 0f, 0f, -1);
			}
			catch { }
		}

		public override void MouseOver(UIMouseEvent evt) {
			base.MouseOver(evt);
			BackgroundColor = UICommon.UI_BLUE_COLOR;
			BorderColor = new Color(89, 116, 213);
		}

		public override void MouseOut(UIMouseEvent evt) {
			base.MouseOut(evt);
			BackgroundColor = new Color(63, 82, 151) * 0.7f;
			BorderColor = new Color(89, 116, 213) * 0.7f;
		}

		internal void DownloadMod(UIMouseEvent evt, UIElement listeningElement) {
			Main.PlaySound(SoundID.MenuTick);
			Interface.downloadMods.SetDownloading("Mod");
			Interface.downloadMods.SetModsToDownload(new List<string>() { mod }, new List<UIModDownloadItem>() { this });
			Interface.modBrowser.updateNeeded = true;
			Main.menuMode = Interface.downloadModsID;
		}

		internal void RequestMoreinfo(UIMouseEvent evt, UIElement listeningElement) {
			Main.PlaySound(SoundID.MenuOpen);
			try {
				ServicePointManager.Expect100Continue = false;
				string url = "http://javid.ddns.net/tModLoader/moddescription.php";
				var values = new NameValueCollection {
					{ "modname", mod },
				};
				using (WebClient client = new WebClient()) {
					ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback((sender, certificate, chain, policyErrors) => { return true; });
					client.UploadValuesCompleted += new UploadValuesCompletedEventHandler(Moreinfo);
					client.UploadValuesAsync(new Uri(url), "POST", values);
				}
			}
			catch (Exception e) {
				UIModBrowser.LogModBrowserException(e);
				return;
			}
		}

		internal void Moreinfo(object sender, UploadValuesCompletedEventArgs e) {
			string description = Language.GetTextValue("tModLoader.ModInfoProblemTryAgain");
			string homepage = "";
			if (!e.Cancelled) {
				string response = Encoding.UTF8.GetString(e.Result);
				JObject joResponse = JObject.Parse(response);
				description = (string)joResponse["description"];
				homepage = (string)joResponse["homepage"];
			}

			Interface.modInfo.SetModName(displayName);
			Interface.modInfo.SetModInfo(description);
			Interface.modInfo.SetMod(installed);
			Interface.modInfo.SetGotoMenu(Interface.modBrowserID);
			Interface.modInfo.SetUrl(homepage);
			Main.menuMode = Interface.modInfoID;
		}
	}

	internal class TimeHelper
	{
		private const int SECOND = 1;
		private const int MINUTE = 60 * SECOND;
		private const int HOUR = 60 * MINUTE;
		private const int DAY = 24 * HOUR;
		private const int MONTH = 30 * DAY;

		// Note: Polish has different plural for numbers ending in 2,3,4. Too complicated to do though.
		public static string HumanTimeSpanString(DateTime yourDate) {
			var ts = new TimeSpan(DateTime.UtcNow.Ticks - yourDate.Ticks);
			double delta = Math.Abs(ts.TotalSeconds);

			if (delta < 1 * MINUTE)
				return ts.Seconds == 1 ? Language.GetTextValue("tModLoader.1SecondAgo") : Language.GetTextValue("tModLoader.XSecondsAgo", ts.Seconds);

			if (delta < 2 * MINUTE)
				return Language.GetTextValue("tModLoader.1MinuteAgo");

			if (delta < 45 * MINUTE)
				return Language.GetTextValue("tModLoader.XMinutesAgo", ts.Minutes);

			if (delta < 90 * MINUTE)
				return Language.GetTextValue("tModLoader.1HourAgo");

			if (delta < 24 * HOUR)
				return Language.GetTextValue("tModLoader.XHoursAgo", ts.Hours);

			if (delta < 48 * HOUR)
				return Language.GetTextValue("tModLoader.1DayAgo");

			if (delta < 30 * DAY)
				return Language.GetTextValue("tModLoader.XDaysAgo", ts.Days);

			if (delta < 12 * MONTH) {
				int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
				return months <= 1 ? Language.GetTextValue("tModLoader.1MonthAgo") : Language.GetTextValue("tModLoader.XMonthsAgo", months);
			}

			int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
			return years <= 1 ? Language.GetTextValue("tModLoader.1YearAgo") : Language.GetTextValue("tModLoader.XYearsAgo", years);
		}
	}
}

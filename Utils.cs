﻿// <copyright file="Utils.cs" company="lokinmodar">
// Copyright (c) lokinmodar. All rights reserved.
// Licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International Public License license.
// </copyright>

using System;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging;
using Echoglossian.Properties;

namespace Echoglossian
{
  public partial class Echoglossian
  {
#if DEBUG
    public void ListCultureInfos()
    {
      using StreamWriter logStream = new(this.configDir + "CultureInfos.txt", append: true);

      CultureInfo[] cus = CultureInfo.GetCultures(CultureTypes.AllCultures);
      foreach (CultureInfo cu in cus)
      {
        logStream.WriteLine(cu.ToString());
      }
    }
#endif

    public string MovePathUp(string path, int noOfLevels)
    {
      string parentPath = path.TrimEnd('/', '\\');
      for (int i = 0; i < noOfLevels; i++)
      {
        if (parentPath != null)
        {
          parentPath = Directory.GetParent(parentPath)?.ToString();
        }
      }

      return parentPath;
    }

    private void ResetSettings()
    {
      this.configuration.Lang = 28;

      this.configuration.FontSize = 24;

      this.configuration.ShowInCutscenes = true;

      this.configuration.TranslateBattleTalk = false;
      this.configuration.TranslateTalk = false;
      this.configuration.TranslateTalkSubtitle = false;
      this.configuration.TranslateToast = false;
      this.configuration.TranslateNpcNames = false;
      this.configuration.TranslateErrorToast = false;
      this.configuration.TranslateQuestToast = false;
      this.configuration.TranslateAreaToast = false;
      this.configuration.TranslateClassChangeToast = false;
      this.configuration.TranslateWideTextToast = false;
      this.configuration.TranslateYesNoScreen = false;
      this.configuration.TranslateCutSceneSelectString = false;
      this.configuration.TranslateSelectString = false;
      this.configuration.TranslateSelectOk = false;
      this.configuration.TranslateToDoList = false;
      this.configuration.TranslateScenarioTree = false;
      this.configuration.TranslateTooltips = false;
      this.configuration.TranslateJournal = false;

      this.configuration.UseImGuiForTalk = false;
      this.configuration.UseImGuiForBattleTalk = false;
      this.configuration.UseImGuiForToasts = false;
      this.configuration.SwapTextsUsingImGui = false;
      this.configuration.ChosenTransEngine = 0;
      this.configuration.TranslateAlreadyTranslatedTexts = false;
      this.configuration.DeeplTranslatorApiKey = string.Empty;
      this.configuration.DeeplTranslatorUsingApiKey = false;
      this.configuration.PluginVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
      this.configuration.Version = 5;

      this.SaveConfig();
      PluginInterface.UiBuilder.AddNotification(Resources.SettingsReset, this.Name, NotificationType.Info, 5000);
    }

    private void FixConfig()
    {
      if (!File.Exists($"{PluginInterface.ConfigFile.FullName}"))
      {
#if DEBUG
        PluginLog.Debug($"Inside config file fixer - Config File Info: {PluginInterface.ConfigFile.FullName}");
#endif

        this.SaveConfig();
        return;
      }

      if (this.configuration.Version >= 5)
      {
        return;
      }

      PluginInterface.ConfigFile.Delete();
      this.SaveConfig();
      this.ResetSettings();
      PluginInterface.GetPluginConfig();
    }

    private void SaveConfig()
    {
      PluginInterface.SavePluginConfig(this.configuration);
    }

    [Flags]
    public enum TransEngines
    {
      Google = 0, // Google Translator (free engine)
      Deepl = 1, // DeepL Translator
      Bing = 2, // Microsoft Bing Translator (free engine)
      Yandex = 3, // Yandex Translator
      GTranslate = 4, // Uses Google, Bing and Yandex (free engines)
      Amazon = 5, // Amazon Translate
      Azure = 6, // Microsoft Azure Translate
      ChatGPT = 7, // Chat GPT
      GoogleCloud = 8, // Google Cloud Translate
      All = Google | Deepl | Bing | Yandex | GTranslate | Amazon | Azure | ChatGPT | GoogleCloud,
    }

    /// <summary>
    /// Creates an image containing the given text.
    /// NOTE: the image should be disposed after use.
    /// </summary>
    /// <param name="text">Text to draw.</param>
    /// <param name="fontOptional">Font to use, defaults to Control.DefaultFont.</param>
    /// <param name="textColorOptional">Text color, defaults to Black.</param>
    /// <param name="backColorOptional">Background color, defaults to white.</param>
    /// <param name="minSizeOptional">Minimum image size, defaults the size required to display the text.</param>
    /// <returns>The image containing the text, which should be disposed after use.</returns>
    public Image DrawText(string text, Font? fontOptional = null, Color? textColorOptional = null, Color? backColorOptional = null, Size? minSizeOptional = null)
    {
#if DEBUG
      PluginLog.Warning("Inside image creation method");
#endif
      PrivateFontCollection pfc = new();
      pfc.AddFontFile($@"{PluginInterface.AssemblyLocation.DirectoryName}{Path.DirectorySeparatorChar}Font{Path.DirectorySeparatorChar}{this.specialFontFileName}");

      Font font = new(pfc.Families[0], this.configuration.FontSize, FontStyle.Regular);
      if (fontOptional != null)
      {
        font = fontOptional;
      }

      Color textColor = Color.White;
      if (textColorOptional != null)
      {
        textColor = (Color)textColorOptional;
      }

      Color backColor = Color.Black;
      if (backColorOptional != null)
      {
        backColor = (Color)backColorOptional;
      }

      Size minSize = Size.Empty;
      if (minSizeOptional != null)
      {
        minSize = (Size)minSizeOptional;
      }

      // first, create a dummy bitmap just to get a graphics object
      SizeF textSize;
      using (Image img = new Bitmap(1, 1))
      {
        using (Graphics drawing = Graphics.FromImage(img))
        {
          // measure the string to see how big the image needs to be
          textSize = drawing.MeasureString(text, font);
          if (!minSize.IsEmpty)
          {
            textSize.Width = textSize.Width > minSize.Width ? textSize.Width : minSize.Width;
            textSize.Height = textSize.Height > minSize.Height ? textSize.Height : minSize.Height;
          }
        }
      }

      // create a new image of the right size
      Image textAsImage = new Bitmap((int)textSize.Width, (int)textSize.Height);
      using (Graphics drawing = Graphics.FromImage(textAsImage))
      {
        // paint the background
        drawing.Clear(backColor);

        // create a brush for the text
        using (Brush textBrush = new SolidBrush(textColor))
        {
          drawing.DrawString(text, font, textBrush, 0, 0);
          drawing.Save();
        }
      }
#if DEBUG
      PluginLog.Warning("Before returning the image created");
#endif
      return textAsImage;
    }

    /// <summary>
    /// Converts Image to byte array.
    /// </summary>
    /// <param name="image">Image to be converted.</param>
    /// <returns>Byte array to be used elsewhere.</returns>
    private byte[] TranslationImageConverter(Image image)
    {
#if DEBUG
      PluginLog.Warning("Conversion to byte");
#endif
      ImageConverter imageConverter = new ImageConverter();
      return (byte[])imageConverter.ConvertTo(image, typeof(byte[]));
    }

    private static bool AssignIfChanged<T>(ref T target, T newValue)
      where T : IEquatable<T>
    {
      if (target.Equals(newValue))
      {
        return false;
      }

      target = newValue;
      return true;
    }

    private static bool IsValidTimeFormat(string time)
    {
      string pattern = @"^(\d{1,3}):(\d{2})$";
      Match match = Regex.Match(time, pattern);

      if (match.Success)
      {
        int minutes = int.Parse(match.Groups[1].Value);
        int seconds = int.Parse(match.Groups[2].Value);
        return minutes < 1000 && seconds < 60;
      }

      return false;
    }
  }
}
﻿// <copyright file="TranslationService.cs" company="lokinmodar">
// Copyright (c) lokinmodar. All rights reserved.
// Licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International Public License license.
// </copyright>

using System;
using System.Threading.Tasks;

using Dalamud.Game.Text.Sanitizer;
using Dalamud.Plugin.Services;

using static Echoglossian.Echoglossian;

namespace Echoglossian
{
  public class TranslationService
  {
    private readonly ITranslator translator;

    private readonly Sanitizer sanitizer;

    public TranslationService(Config config, IPluginLog pluginLog, Sanitizer sanitizer)
    {
      this.sanitizer = sanitizer;
      TransEngines chosenEngine = (TransEngines)config.ChosenTransEngine;

      switch (chosenEngine)
      {
        case TransEngines.Google:
          this.translator = new GoogleTranslator(pluginLog);
          break;
        case TransEngines.Deepl:
          this.translator = new DeepLTranslator(pluginLog, config.DeeplTranslatorUsingApiKey, config.DeeplTranslatorApiKey);
          break;
        case TransEngines.Bing:
          break;
        case TransEngines.Yandex:
          break;
        case TransEngines.GTranslate:
          break;
        case TransEngines.Amazon:
          break;
        case TransEngines.Azure:
          break;
        case TransEngines.ChatGPT:
          break;
        case TransEngines.GoogleCloud:
          break;
        case TransEngines.All:
          break;
        // ... add cases for other translation engines
        default:
          throw new NotSupportedException($"Translation engine {chosenEngine} is not supported.");
      }
    }

    public string Translate(string text, string sourceLanguage, string targetLanguage)
    {
      var (sanitizedText, shouldTranslate) = this.CheckTextToTranslate(text);
      if (!shouldTranslate)
      {
        return sanitizedText;
      }

      string startingEllipsis = string.Empty;

      string parsedText = sanitizedText;
      if (text.StartsWith("..."))
      {
        startingEllipsis = "...";
        parsedText = text.Substring(3);
      }

      string finalDialogueText = this.translator.Translate(parsedText, sourceLanguage, targetLanguage);

      finalDialogueText = finalDialogueText.Replace("a", "x");

      finalDialogueText = !string.IsNullOrEmpty(startingEllipsis)
          ? startingEllipsis + finalDialogueText
          : finalDialogueText;
      return finalDialogueText;
    }

    public async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
      var (sanitizedText, shouldTranslate) = this.CheckTextToTranslate(text);
      if (!shouldTranslate)
      {
        return sanitizedText;
      }

      string startingEllipsis = string.Empty;

      string parsedText = sanitizedText;
      if (text.StartsWith("..."))
      {
        startingEllipsis = "...";
        parsedText = text.Substring(3);
      }

      string finalDialogueText = await this.translator.TranslateAsync(parsedText, sourceLanguage, targetLanguage);

      finalDialogueText = !string.IsNullOrEmpty(startingEllipsis)
          ? startingEllipsis + finalDialogueText
          : finalDialogueText;
      return finalDialogueText;
    }

    private (string SanitizedText, bool ShouldTranslate) CheckTextToTranslate(string text)
    {
      if (string.IsNullOrEmpty(text))
      {
        return (string.Empty, false);
      }

      string sanitizedString = this.sanitizer.Sanitize(text);
      if (string.IsNullOrEmpty(sanitizedString))
      {
        return (string.Empty, false);
      }

      if (sanitizedString == "...")
      {
        return (sanitizedString, false);
      }

      if (sanitizedString == "???")
      {
        return (sanitizedString, false);
      }

      return (sanitizedString, true);
    }
  }
}

using System;
using System.IO;

namespace Rezepte.Web.Services;

/// <summary>
/// Provides access to the Google service account file path and the Gemini API key,
/// resolved from the <c>GOOGLE_APPLICATION_CREDENTIALS</c> / <c>GOOGLE_GEMINI_API_KEY</c>
/// environment variables (with configuration as fallback).
/// </summary>
public interface IGoogleCredentialsProvider
{
    /// <summary>
    /// Returns the full path to the service account file (even if it does not exist).
    /// </summary>
    /// <returns>The resolved service account file path, or an empty string if none is configured.</returns>
    string GetServiceAccountFilePath();

    /// <summary>
    /// Checks whether the service account file exists.
    /// </summary>
    /// <returns><c>true</c> if the resolved service account file exists; otherwise <c>false</c>.</returns>
    bool ServiceAccountFileExists();

    /// <summary>
    /// Returns the API key for Gemini.
    /// </summary>
    /// <returns></returns>
    string GetGeminiApiKey();
}

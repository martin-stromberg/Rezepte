using System;
using System.IO;

namespace Rezepte.Web.Services;

/// <summary>
/// Provider für den Pfad zur Google Service Account JSON Datei.
/// Setzt die Umgebungsvariable __GOOGLE_APPLICATION_CREDENTIALS__ wenn die Datei vorhanden ist.
/// </summary>
public interface IGoogleCredentialsProvider
{
    /// <summary>
    /// Liefert den vollständigen Pfad zur Service-Account-Datei (auch wenn sie nicht existiert).
    /// </summary>
    string GetServiceAccountFilePath();

    /// <summary>
    /// Prüft, ob die Service-Account-Datei vorhanden ist.
    /// </summary>
    bool ServiceAccountFileExists();

    /// <summary>
    /// Liefert den API-Key für Gemini.
    /// </summary>
    /// <returns></returns>
    string GetGeminiApiKey();
}

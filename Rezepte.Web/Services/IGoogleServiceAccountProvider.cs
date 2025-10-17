using System;
using System.IO;

namespace Rezepte.Web.Services;

/// <summary>
/// Provider für den Pfad zur Google Service Account JSON Datei.
/// Setzt die Umgebungsvariable __GOOGLE_APPLICATION_CREDENTIALS__ wenn die Datei vorhanden ist.
/// </summary>
public interface IGoogleServiceAccountProvider
{
    /// <summary>
    /// Liefert den vollständigen Pfad zur Service-Account-Datei (auch wenn sie nicht existiert).
    /// </summary>
    string GetFilePath();

    /// <summary>
    /// Prüft, ob die Service-Account-Datei vorhanden ist.
    /// </summary>
    bool Exists();
}

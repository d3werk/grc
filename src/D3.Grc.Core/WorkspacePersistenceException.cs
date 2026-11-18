namespace D3.Grc.Core;

/// <summary>The change was committed to the working database. Retry SaveAsync to write the project file.</summary>
public sealed class WorkspacePersistenceException(Exception cause)
    : IOException("Die Änderung wurde übernommen. Die Projektdatei konnte nicht vollständig gespeichert werden. "
        + "Das Projekt geöffnet lassen und erneut „Speichern“ wählen. " + cause.Message, cause)
{
}

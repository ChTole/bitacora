namespace Bitacora
{
    /// <summary>Valores que usa el contrato con el servidor (API.md) para los enum de Bitácora.</summary>
    public static class ApiValues
    {
        public static string ToApiValue(this SessionStatus status) =>
            status == SessionStatus.Abandoned ? "abandoned" : "completed";

        /// <summary>Tipo de dato del participante, o null si el modo es anónimo.</summary>
        public static string ToApiValue(this ParticipantMode mode)
        {
            switch (mode)
            {
                case ParticipantMode.Email: return "email";
                case ParticipantMode.FullName: return "full_name";
                case ParticipantMode.DocumentId: return "document_id";
                default: return null;
            }
        }
    }
}
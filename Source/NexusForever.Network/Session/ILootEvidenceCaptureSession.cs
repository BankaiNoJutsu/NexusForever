namespace NexusForever.Network.Session
{
    public interface ILootEvidenceCaptureSession
    {
        void ArmNextLootEvidenceCapture();
        bool TryConsumeNextLootEvidenceCapture();
    }
}

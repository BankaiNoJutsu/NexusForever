namespace NexusForever.Network.Session
{
    public interface IAccountRuntimeEvidenceCaptureSession
    {
        void ArmNextAccountRuntimeEvidenceCapture();
        bool TryConsumeNextAccountRuntimeEvidenceCapture();
    }
}

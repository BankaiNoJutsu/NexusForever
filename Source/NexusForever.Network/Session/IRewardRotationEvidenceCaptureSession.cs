namespace NexusForever.Network.Session
{
    public interface IRewardRotationEvidenceCaptureSession
    {
        void ArmNextRewardRotationEvidenceCapture();
        bool TryConsumeNextRewardRotationEvidenceCapture();
    }
}

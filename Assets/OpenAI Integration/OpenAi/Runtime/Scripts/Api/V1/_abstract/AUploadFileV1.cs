namespace OpenAi.Api.V1
{
    public interface AUploadFileV1
    {
        byte[] GetFileBytes();
        string ToFormDataFields();
    }
}
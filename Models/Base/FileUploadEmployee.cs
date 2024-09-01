namespace PertaminaFileManager.Models.Base
{
    public class FileUploadEmployee
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string UploadedBy { get; set; }
        public DateTime UploadDate { get; set; }
    }
}

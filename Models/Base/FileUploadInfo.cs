namespace PertaminaFileManager.Models.Base
{
    public class fileUploadEmployee
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string UploadedBy { get; set; }
        public DateTime UploadDate { get; set; }
    }
}

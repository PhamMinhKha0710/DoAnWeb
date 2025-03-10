using System;
using System.Collections.Generic;

namespace DoAnWeb.Models;

public partial class RepositoryFile
{
    public int FileId { get; set; }

    public int? RepositoryId { get; set; }

    public string FilePath { get; set; } = null!;

    public string? FileContent { get; set; }

    public string FileHash { get; set; } = null!;

    public virtual Repository? Repository { get; set; }
    
    // Added properties to fix view errors
    public string Content => FileContent ?? string.Empty;
    
    public DateTime? UpdatedDate { get; set; }
    
    public int FileSize { get; set; }
}

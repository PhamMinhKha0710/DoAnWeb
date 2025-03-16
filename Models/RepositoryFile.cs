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

    public DateTime? UpdatedDate { get; set; }

    public long? FileSize { get; set; }

    public string? Content { get; set; }

    public virtual Repository? Repository { get; set; }
}

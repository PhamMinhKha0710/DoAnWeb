using System;
using System.Collections.Generic;

namespace DoAnWeb.Models;

public partial class RepositoryCommit
{
    public int CommitId { get; set; }

    public int? RepositoryId { get; set; }

    public int? AuthorId { get; set; }

    public string CommitMessage { get; set; } = null!;

    public DateTime? CommitDate { get; set; }

    public int? ParentCommitId { get; set; }
    
    public string? CommitHash { get; set; }
    
    public string? Message { get; set; }

    public virtual User? Author { get; set; }

    public virtual User? User { get; set; }

    public virtual Repository? Repository { get; set; }
}

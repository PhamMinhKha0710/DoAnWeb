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

    public virtual User? Author { get; set; }

    public virtual Repository? Repository { get; set; }
    
    // Added properties to fix view errors
    public string Message => CommitMessage;
    
    public string? CommitHash { get; set; }
    
    public User? User => Author;
}

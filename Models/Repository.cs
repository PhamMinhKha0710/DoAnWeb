using System;
using System.Collections.Generic;

namespace DoAnWeb.Models;

public partial class Repository
{
    public int RepositoryId { get; set; }

    public int? OwnerId { get; set; }

    public string RepositoryName { get; set; } = null!;

    public string? Description { get; set; }

    public string Visibility { get; set; } = null!;

    public string DefaultBranch { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual User? Owner { get; set; }

    public virtual ICollection<RepositoryCommit> RepositoryCommits { get; set; } = new List<RepositoryCommit>();

    public virtual ICollection<RepositoryFile> RepositoryFiles { get; set; } = new List<RepositoryFile>();
}

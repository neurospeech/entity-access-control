using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace NeuroSpeech.EntityAccessControl.Tests.Model
{
    public class AppDbContext: BaseDbContext<AppDbContext>
    {
        public long UserID;

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            DbContextEvents<AppDbContext> events,
            IServiceProvider services)
            : base(options, events, services)
        {

        }

        public DbSet<Post> Posts { get; set; }

        public DbSet<Account> Accounts { get; set; }

        public DbSet<Tag> Tags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            var cascadeFKs = modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetForeignKeys())
                .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade);

            foreach (var fk in cascadeFKs)
                fk.DeleteBehavior = DeleteBehavior.Restrict;

            modelBuilder.Entity<PostContentTag>().HasKey(x => new
            {
                x.PostContentID,
                x.Name
            });
            modelBuilder.Entity<PostTag>().HasKey(x => new { 
                x.PostID,
                x.Name
            });
            modelBuilder.Entity<TagKeyword>().HasKey(x => new { 
                x.Name,
                x.Keyword
            });
        }
    }

    [Table("Accounts")]
    public class Account
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long AccountID { get; set; }

        public bool Banned { get; set; }

        public bool IsAdmin { get; set; }

        public string Password { get; set; }
    }

    [Table("Posts")]
    public class Post
    {

        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PostID { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }

        public PostType PostType { get; set; }


        public long AuthorID { get; set; }

        [ForeignKey(nameof(AuthorID))]
        public Account Author { get; set; }

        public ICollection<PostTag> Tags { get; set; }

        public ICollection<PostContent> Contents { get; set; }

        public string AdminComments { get; set; }

        public ICollection<PostAuthor> Authors { get; set; }

    }

    [Table("PostAuthors")]
    public class PostAuthor
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PostAuthorID { get; set; }

        public long PostID { get; set; }

        public long AccountID { get; set; }

        [ForeignKey(nameof(PostID))]
        public Post Post { get; set; }

        [ForeignKey(nameof(AccountID))]
        public Account Account { get; set; }


    }

    public enum PostType
    {
        Page,
        Blog
    }

    [Table("PostTags")]
    public class PostTag
    {
        public long PostID { get; set; }

        [MaxLength(100), Required]
        public string Name { get; set; }

        [ForeignKey(nameof(PostID))]
        public Post Post { get; set; }

        [ForeignKey(nameof(Name))]
        public Tag Tag { get; set; }
    }


    [Table("PostContents")]
    public class PostContent
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PostContentID { get; set; }

        public long PostID { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }

        public ICollection<PostContentTag> Tags { get; set; }

        [ForeignKey(nameof(PostID))]
        public Post Post { get; set; }
    }

    [Table("PostContentTags")]
    public class PostContentTag
    {
        public long PostContentID { get; set; }

        [MaxLength(100), Required]
        public string Name { get; set; }

        [ForeignKey(nameof(PostContentID))]
        public PostContent PostContent { get; set; }

        [ForeignKey(nameof(Name))]
        public Tag Tag { get; set; }
    }

    [Table("Tags")]
    public class Tag
    {
        [Key, MaxLength(100)]
        public string Name { get; set; }

        public ICollection<PostTag> PostTags { get; set; }

        public ICollection<PostContent> PostContents { get; set; }

        public ICollection<TagKeyword> Keywords { get; set; }
    }

    [Table("Keywords")]
    public class TagKeyword
    {
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(200)]
        public string Keyword { get; set; }

        [ForeignKey(nameof(Name))]
        public Tag Tag { get; set; }
    }
}

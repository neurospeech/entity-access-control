using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace NeuroSpeech.EntityAccessControl.Tests.Model
{
    [Keyless]
    public class Pair
    {
        public string Label { get; set; }

        public int Value { get; set; }  
    }

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

        public DbSet<PostActivity> PostActivities { get; set; }

        public DbSet<Campaign> Campaigns { get; set; }

        public DbSet<Account> Accounts { get; set; }

        public DbSet<Tag> Tags { get; set; }

        [ExternalFunction, DbFunction]
        public IQueryable<Pair> GetLabelPairs(int i) => FromExpression(() => GetLabelPairs(i));

        [ExternalFunction]
        public IQueryable<Pair> GetLabelPairs2(int i) => GetLabelPairs(i).Where( x=> x.Value > 0);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // this.RegisterExternalFunctions(modelBuilder);


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

    [Table("PostActivities")]
    public class PostActivity
    {

        [Key,DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ActivityID { get; set; }

        public long AccountID { get; set; }

        public long PostID { get; set; }

        [MaxLength(50)]
        public string ActivityType { get; set; }

        [ForeignKey(nameof(PostID))]
        public Post Post { get; set; }

        [ForeignKey(nameof(AccountID))]
        public Account Account { get; set; }

    }

    [Table("Accounts")]
    public class Account
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long AccountID { get; set; }

        public bool Banned { get; set; }

        public bool IsAdmin { get; set; }

        public string Password { get; set; }

        public List<Post> Posts { get; set; }
    }

    [Table("Campaign")]
    public class Campaign
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CampaignID { get; set; }

        public long AuthorID { get; set; }

        public DateTime DateCreated { get; set; }

        public DateTime DateToSend { get; set; }

        public ICollection<CampaignPost> CampaignPosts { get; set; }
    }

    [Table("CampaignPost")]
    public class CampaignPost
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }

        public long CampaignID { get; set; }

        public long PostID { get; set; }

        [ForeignKey(nameof(PostID))]
        public Post Post { get; set; }

        [ForeignKey(nameof(CampaignID))]
        public Campaign Campaign { get; set; }
    }

    [Table("Posts")]
    public class Post
    {

        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PostID { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }

        public PostType PostType { get; set; }

        public string Description { get; set; }

        public long AuthorID { get; set; }

        [ForeignKey(nameof(AuthorID))]
        public Account Author { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public List<PostTag> Tags { get; set; }

        public ICollection<PostContent> Contents { get; set; }

        public string AdminComments { get; set; }

        public ICollection<PostAuthor> Authors { get; set; }

        public ICollection<Campaign> Campaigns { get; set; }

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

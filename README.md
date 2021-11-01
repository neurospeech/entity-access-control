# Entity Access Control for Entity Framework Core
Entity Access Control framework over Entity Framework Core with Audit and Typed Events

## Features
1. Lambda Expressions for Global Filters
2. Global Filters with Navigation Properties
3. Global Filters for Insert/Update and Delete
4. Auditable, implement your own `IAuditContext`
5. REST API Controller for accessing Entities with Security

## Setup Secure Repository

```c#

[DIRegister(ServiceLifetime.Singleton)]
public class SecurityRules: BaseSecurityRules<IUserInfo> {

    public SecurityRules() {

        // User can access their own projects
        // Manager can access project created created by them or if they are set as Manager for
        // that project
        SetAllFilter<Project>((q, user) => {
            if(user.IsManager) {
                return q.Where(p => p.AccountID == user.AcccountID || p.ManagerID == user.AccountID);
            }
            return q.Where(p => p.AccountID == user.AccountID);
        });
    }

}

/// AppDbContext is your class derived from DbContext
/// IUserInfo is the current user logged in, you can supply your own interface

[DIRegister(ServiceLifetime.Scoped)]
public class SecureRepository: BaseSecureRepository<AppDbContext, IUserInfo> {

    public SecureRepository(
        AppDbContext db,
        IUserInfo user,
        SecurityRules rules)
        : base(db, user, rules)
    {

    }

    public bool SecurityDisabled => user.IsAdmin;

}
```

### Setup Entity Events
```c#

public class AppDbContext: BaseDbContext<AppDbContext> {

    private readonly IUserInfo User;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        AppEvents events,
        IUserInfo user)
        : base(options, events)
    {
        this.User = user;
    }
}

[DIRegister(ServiceLifetime.Singleton)]
public class AppEvents: DbContextEvents<AppDbContext> {

    public AppEvents() {

        Register<Project>(inserting: (db, entity) => {

            // while inserting
            // we want to associate AccountID to currently logged in user id if it is not admin..
            if(db.User.IsAdmin) {
                if (entity.AccountID == 0)
                    entity.AccountID = db.User.AccountID;
            } else {
                entity.AccountID = db.User.AccountID;
            }

            // all events are asynchronous
            return Task.CompletedTask;
        });

    }

}

```
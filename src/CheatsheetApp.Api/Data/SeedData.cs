using CheatsheetApp.Api.Common;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Data;

/// <summary>
/// Sample content applied on first run when the database has no categories.
/// Enabled via the "Database:SeedSampleData" config flag (set by the AppHost in dev).
/// See https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding ("custom initialization logic");
/// HasData is unsuitable here because of the computed tsvector column and dynamic timestamps.
/// </summary>
public static class SeedData
{
    public static async Task ApplyAsync(AppDbContext db)
    {
        if (await db.Categories.AnyAsync())
            return;

        var now = DateTimeOffset.UtcNow;

        var git = NewTag("git");
        var cli = NewTag("cli");
        var containers = NewTag("containers");
        var devops = NewTag("devops");
        var shell = NewTag("shell");
        var postgres = NewTag("postgres");
        var reference = NewTag("reference");

        db.Categories.AddRange(
            new Category
            {
                Name = "Git",
                Slug = SlugGenerator.Generate("Git"),
                Icon = "git-branch",
                SortOrder = 1,
                Cheatsheets =
                [
                    Sheet("Git Basics", GitBasics, now, git, cli),
                    Sheet("Git Branching", GitBranching, now, git, cli),
                ],
            },
            new Category
            {
                Name = "Docker",
                Slug = SlugGenerator.Generate("Docker"),
                Icon = "container",
                SortOrder = 2,
                Cheatsheets =
                [
                    Sheet("Docker CLI", DockerCli, now, containers, devops, cli),
                    Sheet("Dockerfile Reference", DockerfileReference, now, containers, reference),
                ],
            },
            new Category
            {
                Name = "Linux & Shell",
                Slug = SlugGenerator.Generate("Linux & Shell"),
                Icon = "terminal",
                SortOrder = 3,
                Cheatsheets =
                [
                    Sheet("Bash Essentials", BashEssentials, now, shell, cli),
                    Sheet("File Permissions", FilePermissions, now, shell, reference),
                ],
            },
            new Category
            {
                Name = "Databases",
                Slug = SlugGenerator.Generate("Databases"),
                Icon = "database",
                SortOrder = 4,
                Cheatsheets =
                [
                    Sheet("PostgreSQL Quick Reference", PostgresQuickReference, now, postgres, reference),
                    HtmlSheet("HTML Entities", HtmlEntities, now, reference),
                ],
            });

        await db.SaveChangesAsync();
    }

    private static Tag NewTag(string name) => new()
    {
        Name = name,
        Slug = SlugGenerator.Generate(name),
    };

    private static Cheatsheet Sheet(string title, string content, DateTimeOffset now, params Tag[] tags) => new()
    {
        Title = title,
        Slug = SlugGenerator.Generate(title),
        ContentType = ContentTypes.Markdown,
        Content = content,
        CreatedAt = now,
        UpdatedAt = now,
        Tags = [.. tags],
    };

    private static Cheatsheet HtmlSheet(string title, string content, DateTimeOffset now, params Tag[] tags) => new()
    {
        Title = title,
        Slug = SlugGenerator.Generate(title),
        ContentType = ContentTypes.Html,
        Content = content,
        CreatedAt = now,
        UpdatedAt = now,
        Tags = [.. tags],
    };

    private const string GitBasics = """
        # Git Basics

        ## Setup

        ```bash
        git config --global user.name "Your Name"
        git config --global user.email "you@example.com"
        ```

        ## Everyday commands

        | Command | Description |
        |---------|-------------|
        | `git status` | Show working tree status |
        | `git add <file>` | Stage a file |
        | `git add -p` | Stage interactively, hunk by hunk |
        | `git commit -m "msg"` | Commit staged changes |
        | `git pull --rebase` | Fetch and rebase local commits on top |
        | `git push` | Push commits to the remote |
        | `git log --oneline --graph` | Compact history with branch graph |

        ## Undoing things

        ```bash
        git restore <file>          # discard unstaged changes to a file
        git restore --staged <file> # unstage, keep changes in working tree
        git commit --amend          # fix the last commit (message or content)
        git revert <sha>            # new commit that undoes <sha>
        ```

        > Tip: `git reflog` shows where HEAD has been — almost nothing is truly lost.
        """;

    private const string GitBranching = """
        # Git Branching

        ## Branch lifecycle

        ```bash
        git switch -c feature/my-feature   # create and switch
        git switch main                    # go back
        git branch -d feature/my-feature   # delete (merged only)
        git branch -D feature/my-feature   # force delete
        ```

        ## Merging vs rebasing

        - **Merge** keeps history as it happened: `git merge feature` from the target branch.
        - **Rebase** rewrites your commits onto a new base: `git rebase main` from the feature branch.
        - Never rebase commits that are already pushed and shared.

        ## Stashing

        ```bash
        git stash              # shelve dirty working tree
        git stash pop          # re-apply and drop the latest stash
        git stash list         # see what is shelved
        ```

        ## Resolving conflicts

        1. `git status` shows conflicted files.
        2. Edit files, look for `<<<<<<<` markers.
        3. `git add` the resolved files.
        4. Continue with `git merge --continue` or `git rebase --continue`.
        """;

    private const string DockerCli = """
        # Docker CLI

        ## Containers

        ```bash
        docker run -d --name web -p 8080:80 nginx   # run detached, map port
        docker ps                                   # running containers
        docker ps -a                                # include stopped
        docker logs -f web                          # follow logs
        docker exec -it web sh                      # shell into container
        docker stop web && docker rm web            # stop and remove
        ```

        ## Images

        ```bash
        docker build -t myapp:latest .   # build from Dockerfile
        docker images                    # list local images
        docker rmi myapp:latest          # remove an image
        docker pull postgres:17          # fetch from registry
        ```

        ## Cleanup

        ```bash
        docker system prune     # remove stopped containers, dangling images, unused networks
        docker volume prune     # remove unused volumes (careful: data loss)
        ```

        ## Compose

        ```bash
        docker compose up -d     # start services in background
        docker compose down      # stop and remove
        docker compose logs -f   # follow all service logs
        ```
        """;

    private const string DockerfileReference = """
        # Dockerfile Reference

        ## Common instructions

        | Instruction | Purpose |
        |-------------|---------|
        | `FROM` | Base image (first non-comment line) |
        | `WORKDIR` | Set working directory for following instructions |
        | `COPY` | Copy files from build context into the image |
        | `RUN` | Execute a command at build time (new layer) |
        | `ENV` | Set an environment variable |
        | `EXPOSE` | Document the port the app listens on |
        | `ENTRYPOINT` / `CMD` | What runs when the container starts |

        ## Multi-stage build (.NET example)

        ```dockerfile
        FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
        WORKDIR /src
        COPY . .
        RUN dotnet publish -c Release -o /app

        FROM mcr.microsoft.com/dotnet/aspnet:10.0
        WORKDIR /app
        COPY --from=build /app .
        ENTRYPOINT ["dotnet", "MyApp.dll"]
        ```

        ## Tips

        - Order instructions from least to most frequently changing to maximize layer cache hits.
        - Use a `.dockerignore` file to keep the build context small.
        - Prefer `COPY` over `ADD` unless you need archive extraction.
        """;

    private const string BashEssentials = """
        # Bash Essentials

        ## Navigation & files

        ```bash
        cd -            # jump back to previous directory
        ls -lah         # long listing, all files, human sizes
        find . -name "*.log" -mtime +7   # logs older than a week
        du -sh *        # size of each item in current dir
        ```

        ## Pipes & redirection

        ```bash
        command > out.txt        # stdout to file (overwrite)
        command >> out.txt       # append
        command 2>&1 | tee log   # stdout+stderr to screen and file
        cmd1 && cmd2             # run cmd2 only if cmd1 succeeded
        ```

        ## Text processing

        ```bash
        grep -rn "pattern" src/        # recursive search with line numbers
        sed -i 's/old/new/g' file      # in-place replace
        awk '{print $2}' file          # print second column
        sort | uniq -c | sort -rn      # frequency count, descending
        ```

        ## Keyboard shortcuts

        | Keys | Action |
        |------|--------|
        | `Ctrl+R` | Search command history |
        | `Ctrl+A` / `Ctrl+E` | Start / end of line |
        | `Ctrl+W` | Delete word before cursor |
        | `Ctrl+L` | Clear screen |
        """;

    private const string FilePermissions = """
        # File Permissions

        ## Reading `ls -l` output

        ```
        -rwxr-xr--  1 alice staff  4096 Jun 10 10:00 script.sh
         ↑↑↑ ↑↑↑ ↑↑↑
         user group other
        ```

        - `r` = read (4), `w` = write (2), `x` = execute (1)

        ## chmod

        ```bash
        chmod 755 script.sh    # rwx r-x r-x  (typical executable)
        chmod 644 file.txt     # rw- r-- r--  (typical file)
        chmod 600 id_rsa       # rw- --- ---  (private key)
        chmod +x script.sh     # add execute for everyone
        chmod u+w,g-w file     # symbolic: add for user, remove for group
        ```

        ## Ownership

        ```bash
        chown alice file          # change owner
        chown alice:staff file    # change owner and group
        chown -R alice:staff dir  # recursive
        ```

        ## Common permission numbers

        | Mode | Meaning |
        |------|---------|
        | 777 | Everyone can do everything (avoid) |
        | 755 | Owner full, others read+execute |
        | 644 | Owner read+write, others read |
        | 600 | Owner read+write only |
        """;

    private const string PostgresQuickReference = """
        # PostgreSQL Quick Reference

        ## psql meta-commands

        | Command | Description |
        |---------|-------------|
        | `\l` | List databases |
        | `\c dbname` | Connect to a database |
        | `\dt` | List tables |
        | `\d tablename` | Describe a table |
        | `\x` | Toggle expanded (vertical) output |
        | `\q` | Quit |

        ## Useful queries

        ```sql
        -- Table sizes, largest first
        SELECT relname, pg_size_pretty(pg_total_relation_size(relid))
        FROM pg_catalog.pg_statio_user_tables
        ORDER BY pg_total_relation_size(relid) DESC;

        -- Currently running queries
        SELECT pid, state, query_start, query
        FROM pg_stat_activity
        WHERE state <> 'idle';

        -- Kill a query
        SELECT pg_cancel_backend(pid);     -- polite
        SELECT pg_terminate_backend(pid);  -- forceful
        ```

        ## Full-text search

        ```sql
        SELECT title
        FROM cheatsheets
        WHERE search_vector @@ websearch_to_tsquery('english', 'docker compose')
        ORDER BY ts_rank(search_vector, websearch_to_tsquery('english', 'docker compose')) DESC;
        ```

        ## Upsert

        ```sql
        INSERT INTO tags (name, slug) VALUES ('git', 'git')
        ON CONFLICT (slug) DO UPDATE SET name = EXCLUDED.name;
        ```
        """;

    private const string HtmlEntities = """
        <h1>HTML Entities</h1>
        <p>Characters that must be escaped in HTML, and a few that are just handy.</p>
        <table>
          <thead>
            <tr><th>Character</th><th>Entity</th><th>Notes</th></tr>
          </thead>
          <tbody>
            <tr><td>&lt;</td><td><code>&amp;lt;</code></td><td>Must escape in text content</td></tr>
            <tr><td>&gt;</td><td><code>&amp;gt;</code></td><td>Escape for symmetry/safety</td></tr>
            <tr><td>&amp;</td><td><code>&amp;amp;</code></td><td>Must escape everywhere</td></tr>
            <tr><td>"</td><td><code>&amp;quot;</code></td><td>Must escape inside attributes</td></tr>
            <tr><td>&nbsp;</td><td><code>&amp;nbsp;</code></td><td>Non-breaking space</td></tr>
            <tr><td>&mdash;</td><td><code>&amp;mdash;</code></td><td>Em dash</td></tr>
            <tr><td>&copy;</td><td><code>&amp;copy;</code></td><td>Copyright sign</td></tr>
            <tr><td>&rarr;</td><td><code>&amp;rarr;</code></td><td>Right arrow</td></tr>
          </tbody>
        </table>
        <p>Rule of thumb: escape <code>&amp;</code>, <code>&lt;</code> in all text, and additionally
        <code>"</code> inside double-quoted attribute values.</p>
        """;
}

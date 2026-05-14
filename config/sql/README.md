# SQL Server Operational Scripts

## `01_enable_tde.sql` — Transparent Data Encryption (Egypt PDPL)

Enables TDE on the `GymApp` database with an AES-256 database encryption key.

### When to run

After `GymApp` database exists — i.e. after first `dotnet ef database update` (or first API startup that applies migrations). Running before the database exists will fail at step 3.

### Required variables

The script uses `sqlcmd`-style `:setvar` placeholders. Pass them via `-v`:

| Variable | Purpose |
|---|---|
| `TdeMasterKeyPassword` | Password for the service master key in `master`. Store in your secrets vault. |
| `TdeCertBackupPassword` | Password that protects the certificate private-key backup file. |

### Local / dev

```bash
docker compose up -d gymapp-db
# wait for the API to run migrations, OR run them yourself:
dotnet ef database update --project src/Modules/Identity/GymApp.Modules.Identity.Infrastructure  # etc.

docker compose cp config/sql/01_enable_tde.sql gymapp-db:/tmp/enable_tde.sql
docker compose exec gymapp-db mkdir -p /var/opt/mssql/tde-backup
docker compose exec gymapp-db /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" -C \
    -v TdeMasterKeyPassword="<strong-password>" \
    -v TdeCertBackupPassword="<strong-password>" \
    -i /tmp/enable_tde.sql
```

### Production checklist

1. Run the script against the production SQL instance using a break-glass admin account.
2. **Copy `/var/opt/mssql/tde-backup/GymAppTdeCert.cer` + `.pvk` off the server** into your KMS / offline secure storage. **Delete them from the server afterwards.** Without these files, a backup cannot be restored on a new instance.
3. Verify `encryption_state = 3` in the query at the end of the script.
4. Record both passwords in the secrets vault — they are required for disaster recovery.

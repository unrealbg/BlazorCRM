namespace Crm.Infrastructure.Persistence.Migrations
{
    using Crm.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Migrations;
    using NpgsqlTypes;

#nullable disable

    [DbContext(typeof(CrmDbContext))]
    [Migration("20260120000000_AddSearchVectors")]
    public partial class AddSearchVectors : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
                        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");
                        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

                        migrationBuilder.AddColumn<NpgsqlTsVector>(
                                name: "SearchVector",
                                table: "Companies",
                                type: "tsvector",
                                nullable: true);

                        migrationBuilder.AddColumn<NpgsqlTsVector>(
                                name: "SearchVector",
                                table: "Contacts",
                                type: "tsvector",
                                nullable: true);

                        migrationBuilder.AddColumn<NpgsqlTsVector>(
                                name: "SearchVector",
                                table: "Deals",
                                type: "tsvector",
                                nullable: true);

                        migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION crm_companies_searchvector_update() RETURNS trigger AS $$
BEGIN
    NEW.""SearchVector"" := to_tsvector('simple', unaccent(
        coalesce(NEW.""Name"", '') || ' ' ||
        coalesce(NEW.""Industry"", '') || ' ' ||
        coalesce(NEW.""Address"", '') || ' ' ||
        coalesce(NEW.""Tags"", '')
    ));
    RETURN NEW;
END
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION crm_contacts_searchvector_update() RETURNS trigger AS $$
BEGIN
    NEW.""SearchVector"" := to_tsvector('simple', unaccent(
        coalesce(NEW.""FirstName"", '') || ' ' ||
        coalesce(NEW.""LastName"", '') || ' ' ||
        coalesce(NEW.""Email"", '') || ' ' ||
        coalesce(NEW.""Phone"", '') || ' ' ||
        coalesce(NEW.""Position"", '') || ' ' ||
        coalesce(NEW.""Tags"", '')
    ));
    RETURN NEW;
END
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION crm_deals_searchvector_update() RETURNS trigger AS $$
BEGIN
    NEW.""SearchVector"" := to_tsvector('simple', unaccent(
        coalesce(NEW.""Title"", '') || ' ' ||
        coalesce(NEW.""Currency"", '')
    ));
    RETURN NEW;
END
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_companies_searchvector ON ""Companies"";
CREATE TRIGGER trg_companies_searchvector
BEFORE INSERT OR UPDATE ON ""Companies""
FOR EACH ROW EXECUTE FUNCTION crm_companies_searchvector_update();

DROP TRIGGER IF EXISTS trg_contacts_searchvector ON ""Contacts"";
CREATE TRIGGER trg_contacts_searchvector
BEFORE INSERT OR UPDATE ON ""Contacts""
FOR EACH ROW EXECUTE FUNCTION crm_contacts_searchvector_update();

DROP TRIGGER IF EXISTS trg_deals_searchvector ON ""Deals"";
CREATE TRIGGER trg_deals_searchvector
BEFORE INSERT OR UPDATE ON ""Deals""
FOR EACH ROW EXECUTE FUNCTION crm_deals_searchvector_update();

UPDATE ""Companies"" SET ""SearchVector"" = to_tsvector('simple', unaccent(
    coalesce(""Name"", '') || ' ' || coalesce(""Industry"", '') || ' ' || coalesce(""Address"", '') || ' ' || coalesce(""Tags"", '')
));

UPDATE ""Contacts"" SET ""SearchVector"" = to_tsvector('simple', unaccent(
    coalesce(""FirstName"", '') || ' ' || coalesce(""LastName"", '') || ' ' || coalesce(""Email"", '') || ' ' || coalesce(""Phone"", '') || ' ' || coalesce(""Position"", '') || ' ' || coalesce(""Tags"", '')
));

UPDATE ""Deals"" SET ""SearchVector"" = to_tsvector('simple', unaccent(
    coalesce(""Title"", '') || ' ' || coalesce(""Currency"", '')
));
                        ");

                        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_companies_searchvector ON \"Companies\" USING GIN (\"SearchVector\");");
                        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_contacts_searchvector ON \"Contacts\" USING GIN (\"SearchVector\");");
                        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_deals_searchvector ON \"Deals\" USING GIN (\"SearchVector\");");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_companies_searchvector;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_contacts_searchvector;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_deals_searchvector;");

            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_companies_searchvector ON \"Companies\";");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_contacts_searchvector ON \"Contacts\";");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_deals_searchvector ON \"Deals\";");

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS crm_companies_searchvector_update();");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS crm_contacts_searchvector_update();");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS crm_deals_searchvector_update();");

            migrationBuilder.DropColumn(name: "SearchVector", table: "Companies");
            migrationBuilder.DropColumn(name: "SearchVector", table: "Contacts");
            migrationBuilder.DropColumn(name: "SearchVector", table: "Deals");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eatah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPoultryCategoryAndUpdateLivsmedelsverket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reclassify existing chicken meals from Meat to Poultry.
            migrationBuilder.Sql(@"
                UPDATE meals
                SET category = 'Poultry'
                WHERE category = 'Meat'
                  AND name IN (
                    'Blomkålsris med kyckling',
                    'Kycklingcurry med ris',
                    'Stekt kycklinglårfilé med ugnsrostade grönsaker'
                  );
            ");

            // Tighten the Livsmedelsverket meat-rule description to reference red meat explicitly.
            migrationBuilder.Sql(@"
                UPDATE diet_rules
                SET description = 'Begränsa rött och processat kött till högst 3 gånger per vecka.'
                WHERE category = 'Meat'
                  AND diet_profile_id IN (SELECT id FROM diet_profiles WHERE name = 'Livsmedelsverket');
            ");

            // Add a Poultry rule to the Livsmedelsverket profile if it does not already exist.
            migrationBuilder.Sql(@"
                INSERT INTO diet_rules (id, category, min_per_week, max_per_week, description, diet_profile_id)
                SELECT 'b7e8c9d4-1f2a-4d3e-9c5a-7e8f1a2b3c4d'::uuid,
                       'Poultry',
                       1,
                       3,
                       'Ät fågel med måtta, förslagsvis 1–3 gånger per vecka.',
                       p.id
                FROM diet_profiles p
                WHERE p.name = 'Livsmedelsverket'
                  AND NOT EXISTS (
                      SELECT 1 FROM diet_rules r
                      WHERE r.diet_profile_id = p.id AND r.category = 'Poultry'
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM diet_rules
                WHERE category = 'Poultry'
                  AND diet_profile_id IN (SELECT id FROM diet_profiles WHERE name = 'Livsmedelsverket');
            ");

            migrationBuilder.Sql(@"
                UPDATE diet_rules
                SET description = 'Begränsa rött och processat kött.'
                WHERE category = 'Meat'
                  AND diet_profile_id IN (SELECT id FROM diet_profiles WHERE name = 'Livsmedelsverket');
            ");

            migrationBuilder.Sql(@"
                UPDATE meals
                SET category = 'Meat'
                WHERE category = 'Poultry'
                  AND name IN (
                    'Blomkålsris med kyckling',
                    'Kycklingcurry med ris',
                    'Stekt kycklinglårfilé med ugnsrostade grönsaker'
                  );
            ");
        }
    }
}

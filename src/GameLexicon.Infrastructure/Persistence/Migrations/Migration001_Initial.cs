using Microsoft.Data.Sqlite;

namespace GameLexicon.Infrastructure.Persistence.Migrations;

public sealed class Migration001_Initial : IDatabaseMigration
{
    public int Version => 1;

    public async Task ApplyAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = Sql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private const string Sql = """
        CREATE TABLE captures (
            id TEXT PRIMARY KEY,
            captured_at_utc TEXT NOT NULL,
            source_window_title TEXT NOT NULL DEFAULT '',
            source_process_name TEXT NOT NULL DEFAULT '',
            game_title TEXT,
            image_path TEXT NOT NULL,
            pixel_width INTEGER NOT NULL,
            pixel_height INTEGER NOT NULL,
            status INTEGER NOT NULL,
            error_message TEXT
        );

        CREATE TABLE ocr_regions (
            id TEXT PRIMARY KEY,
            capture_id TEXT NOT NULL,
            x INTEGER NOT NULL,
            y INTEGER NOT NULL,
            width INTEGER NOT NULL,
            height INTEGER NOT NULL,
            raw_text TEXT NOT NULL DEFAULT '',
            corrected_text TEXT NOT NULL DEFAULT '',
            created_at_utc TEXT NOT NULL,
            FOREIGN KEY (capture_id) REFERENCES captures(id) ON DELETE CASCADE
        );

        CREATE TABLE ocr_tokens (
            id TEXT PRIMARY KEY,
            ocr_region_id TEXT NOT NULL,
            text TEXT NOT NULL,
            confidence REAL NOT NULL,
            x INTEGER NOT NULL,
            y INTEGER NOT NULL,
            width INTEGER NOT NULL,
            height INTEGER NOT NULL,
            block_index INTEGER NOT NULL,
            paragraph_index INTEGER NOT NULL,
            line_index INTEGER NOT NULL,
            word_index INTEGER NOT NULL,
            FOREIGN KEY (ocr_region_id) REFERENCES ocr_regions(id) ON DELETE CASCADE
        );

        CREATE TABLE sentence_examples (
            id TEXT PRIMARY KEY,
            capture_id TEXT NOT NULL,
            ocr_region_id TEXT,
            sentence_text TEXT NOT NULL,
            normalized_sentence TEXT NOT NULL,
            target_start INTEGER NOT NULL,
            target_length INTEGER NOT NULL,
            screenshot_crop_path TEXT NOT NULL DEFAULT '',
            game_title TEXT,
            created_at_utc TEXT NOT NULL,
            FOREIGN KEY (capture_id) REFERENCES captures(id) ON DELETE RESTRICT,
            FOREIGN KEY (ocr_region_id) REFERENCES ocr_regions(id) ON DELETE SET NULL
        );

        CREATE TABLE vocabulary_entries (
            id TEXT PRIMARY KEY,
            headword TEXT NOT NULL,
            normalized_headword TEXT NOT NULL,
            entry_type INTEGER NOT NULL,
            part_of_speech TEXT,
            phonetic TEXT,
            definition_english TEXT,
            translation_chinese TEXT,
            notes TEXT,
            is_archived INTEGER NOT NULL DEFAULT 0,
            created_at_utc TEXT NOT NULL,
            updated_at_utc TEXT NOT NULL
        );

        CREATE UNIQUE INDEX ux_vocabulary_entries_normalized_active
        ON vocabulary_entries(normalized_headword)
        WHERE is_archived = 0;

        CREATE TABLE entry_examples (
            entry_id TEXT NOT NULL,
            example_id TEXT NOT NULL,
            is_primary INTEGER NOT NULL DEFAULT 0,
            sort_order INTEGER NOT NULL DEFAULT 0,
            PRIMARY KEY (entry_id, example_id),
            FOREIGN KEY (entry_id) REFERENCES vocabulary_entries(id) ON DELETE CASCADE,
            FOREIGN KEY (example_id) REFERENCES sentence_examples(id) ON DELETE CASCADE
        );

        CREATE TABLE tags (
            id TEXT PRIMARY KEY,
            name TEXT NOT NULL,
            normalized_name TEXT NOT NULL UNIQUE
        );

        CREATE TABLE entry_tags (
            entry_id TEXT NOT NULL,
            tag_id TEXT NOT NULL,
            PRIMARY KEY (entry_id, tag_id),
            FOREIGN KEY (entry_id) REFERENCES vocabulary_entries(id) ON DELETE CASCADE,
            FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE
        );

        CREATE TABLE review_cards (
            id TEXT PRIMARY KEY,
            entry_id TEXT NOT NULL,
            card_type INTEGER NOT NULL,
            due_at_utc TEXT NOT NULL,
            repetition INTEGER NOT NULL DEFAULT 0,
            interval_days REAL NOT NULL DEFAULT 0,
            ease_factor REAL NOT NULL DEFAULT 2.5,
            lapse_count INTEGER NOT NULL DEFAULT 0,
            last_reviewed_at_utc TEXT,
            is_suspended INTEGER NOT NULL DEFAULT 0,
            FOREIGN KEY (entry_id) REFERENCES vocabulary_entries(id) ON DELETE CASCADE
        );

        CREATE UNIQUE INDEX ux_review_cards_entry_type
        ON review_cards(entry_id, card_type);

        CREATE INDEX ix_review_cards_due
        ON review_cards(is_suspended, due_at_utc);

        CREATE TABLE review_logs (
            id TEXT PRIMARY KEY,
            review_card_id TEXT NOT NULL,
            reviewed_at_utc TEXT NOT NULL,
            grade INTEGER NOT NULL,
            previous_interval_days REAL NOT NULL,
            new_interval_days REAL NOT NULL,
            previous_ease_factor REAL NOT NULL,
            new_ease_factor REAL NOT NULL,
            response_milliseconds INTEGER,
            FOREIGN KEY (review_card_id) REFERENCES review_cards(id) ON DELETE CASCADE
        );

        CREATE TABLE app_settings (
            key TEXT PRIMARY KEY,
            value_json TEXT NOT NULL,
            updated_at_utc TEXT NOT NULL
        );
        """;
}

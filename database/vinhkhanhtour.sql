-- ============================================================
-- Migration: thêm cột Value, Lat, Lng vào bảng analytics
-- Tương thích MySQL 5.7+ và MySQL 8.0+
-- ============================================================

-- Dùng procedure để kiểm tra cột trước khi ADD (tránh lỗi "Duplicate column")
DROP PROCEDURE IF EXISTS migrate_analytics_v2;

DELIMITER $$
CREATE PROCEDURE migrate_analytics_v2()
BEGIN
    -- Thêm cột Value nếu chưa có
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME   = 'analytics'
          AND COLUMN_NAME  = 'Value'
    ) THEN
        ALTER TABLE analytics
            ADD COLUMN `Value` DOUBLE NOT NULL DEFAULT 0
            COMMENT 'Duration tính bằng giây khi EventType = audio_*';
    END IF;

    -- Thêm cột Lat nếu chưa có
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME   = 'analytics'
          AND COLUMN_NAME  = 'Lat'
    ) THEN
        ALTER TABLE analytics
            ADD COLUMN `Lat` DOUBLE NOT NULL DEFAULT 0
            COMMENT 'Vĩ độ GPS tại thời điểm ghi event (0 = không có)';
    END IF;

    -- Thêm cột Lng nếu chưa có
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME   = 'analytics'
          AND COLUMN_NAME  = 'Lng'
    ) THEN
        ALTER TABLE analytics
            ADD COLUMN `Lng` DOUBLE NOT NULL DEFAULT 0
            COMMENT 'Kinh độ GPS tại thời điểm ghi event (0 = không có)';
    END IF;

    -- Thêm index heatmap nếu chưa có
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME   = 'analytics'
          AND INDEX_NAME   = 'idx_analytics_latlng'
    ) THEN
        ALTER TABLE analytics
            ADD INDEX idx_analytics_latlng (Lat, Lng);
    END IF;

    -- Thêm index eventtype nếu chưa có
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME   = 'analytics'
          AND INDEX_NAME   = 'idx_analytics_eventtype'
    ) THEN
        ALTER TABLE analytics
            ADD INDEX idx_analytics_eventtype (EventType);
    END IF;
END$$
DELIMITER ;

-- Chạy procedure
CALL migrate_analytics_v2();

-- Dọn dẹp
DROP PROCEDURE IF EXISTS migrate_analytics_v2;

-- Kiểm tra kết quả
DESCRIBE analytics;
SELECT Id, Username, Password, Role FROM users;
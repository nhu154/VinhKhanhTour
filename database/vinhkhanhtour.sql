-- Migration: Add user_locations table for real-time heatmap tracking
-- Each user gets 1 record that gets updated when they send GPS data

CREATE TABLE IF NOT EXISTS user_locations (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    UserId INT NOT NULL,
    Latitude DOUBLE NOT NULL,
    Longitude DOUBLE NOT NULL,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uk_userid (UserId),
    FOREIGN KEY (UserId) REFERENCES users(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Index for finding recent locations (within 5 minutes)
CREATE INDEX idx_updated_at ON user_locations(UpdatedAt DESC);

INSERT INTO user_locations (UserId, Latitude, Longitude, UpdatedAt) 
SELECT DISTINCT 
    COALESCE(RestaurantId, 0),
    AVG(Lat) as Latitude,
    AVG(Lng) as Longitude,
    MAX(Timestamp) as UpdatedAt
FROM analytics 
WHERE Lat != 0 AND Lng != 0 AND RestaurantId IS NOT NULL
GROUP BY RestaurantId
ON DUPLICATE KEY UPDATE 
    Latitude = VALUES(Latitude),
    Longitude = VALUES(Longitude),
    UpdatedAt = VALUES(UpdatedAt);
user_devicesuser_live_locations
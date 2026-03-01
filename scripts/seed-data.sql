-- ============================================================
-- SADC Order Management — Seed Script
-- Generates ≥ 1,000 orders across SADC countries and currencies
-- ============================================================

SET NOCOUNT ON;
BEGIN TRANSACTION;

-- 1. Seed Customers (one per SADC country)
DECLARE @Customers TABLE (Id UNIQUEIDENTIFIER, Name NVARCHAR(200), Email NVARCHAR(200), CountryCode NVARCHAR(2));

INSERT INTO @Customers VALUES
(NEWID(), 'Angola Trading Co.',       'trade@angola.co.ao',       'AO'),
(NEWID(), 'Botswana Imports Ltd.',     'info@bwimports.co.bw',     'BW'),
(NEWID(), 'Comoros Exports SARL',      'sales@comoros-ex.km',      'KM'),
(NEWID(), 'Congo Resources SPRL',     'ops@congores.cd',          'CD'),
(NEWID(), 'Eswatini Products (Pty)',   'orders@swaziprod.co.sz',   'SZ'),
(NEWID(), 'Lesotho Textiles Ltd.',     'sales@lstextiles.co.ls',   'LS'),
(NEWID(), 'Madagascar Vanille SA',     'contact@mgvanille.mg',     'MG'),
(NEWID(), 'Malawi Tea Estates',        'tea@mwtea.co.mw',          'MW'),
(NEWID(), 'Mauritius Tech Ltd.',       'info@mutech.mu',           'MU'),
(NEWID(), 'Mozambique Mining SA',      'ops@mzmining.co.mz',       'MZ'),
(NEWID(), 'Namibia Diamonds (Pty)',    'gems@nadiamonds.com.na',   'NA'),
(NEWID(), 'Seychelles Fisheries',      'catch@scfish.sc',          'SC'),
(NEWID(), 'South Africa Steel (Pty)',  'steel@zasteel.co.za',      'ZA'),
(NEWID(), 'Tanzania Coffee Board',     'coffee@tzcoffee.co.tz',    'TZ'),
(NEWID(), 'Zambia Copper Corp.',       'copper@zmcopper.co.zm',    'ZM'),
(NEWID(), 'Zimbabwe Tobacco Ltd.',     'tobacco@zwtobacco.co.zw',  'ZW'),
-- Additional customers for variety
(NEWID(), 'Cape Town Logistics',       'logistics@ctlog.co.za',    'ZA'),
(NEWID(), 'Johannesburg Wholesale',    'wholesale@jhbws.co.za',    'ZA'),
(NEWID(), 'Windhoek Supply Chain',     'supply@whksc.com.na',      'NA'),
(NEWID(), 'Gaborone Electronics',      'elec@gabelec.co.bw',       'BW'),
(NEWID(), 'Maputo Shipping',           'ship@mpship.co.mz',        'MZ'),
(NEWID(), 'Lusaka Groceries Ltd.',     'grocery@lskgroc.co.zm',    'ZM'),
(NEWID(), 'Harare Manufacturing',      'mfg@hremfg.co.zw',         'ZW'),
(NEWID(), 'Dar es Salaam Traders',     'trade@dstrade.co.tz',      'TZ');

INSERT INTO Customers (Id, Name, Email, CountryCode, CreatedAtUtc)
SELECT Id, Name, Email, CountryCode, DATEADD(DAY, -ABS(CHECKSUM(NEWID())) % 365, GETUTCDATE())
FROM @Customers;

-- 2. Build a mapping of countries to currencies
DECLARE @CountryCurrency TABLE (CountryCode NVARCHAR(2), CurrencyCode NVARCHAR(3));
INSERT INTO @CountryCurrency VALUES
('AO','AOA'), ('BW','BWP'), ('KM','KMF'), ('CD','CDF'),
('SZ','SZL'), ('SZ','ZAR'), ('LS','LSL'), ('LS','ZAR'),
('MG','MGA'), ('MW','MWK'), ('MU','MUR'), ('MZ','MZN'),
('NA','NAD'), ('NA','ZAR'), ('SC','SCR'), ('ZA','ZAR'),
('TZ','TZS'), ('ZM','ZMW'), ('ZW','USD'), ('ZW','ZWL');

-- 3. Generate 1,200+ orders with line items
DECLARE @i INT = 1;
DECLARE @CustomerId UNIQUEIDENTIFIER;
DECLARE @OrderId UNIQUEIDENTIFIER;
DECLARE @Currency NVARCHAR(3);
DECLARE @Status NVARCHAR(20);
DECLARE @OrderDate DATETIME2;
DECLARE @NumItems INT;
DECLARE @j INT;
DECLARE @TotalAmount DECIMAL(18,2);
DECLARE @UnitPrice DECIMAL(18,2);
DECLARE @Quantity INT;

DECLARE @Products TABLE (Sku NVARCHAR(50));
INSERT INTO @Products VALUES
('PROD-COFFEE-001'), ('PROD-TEA-002'), ('PROD-COPPER-003'), ('PROD-DIAMOND-004'),
('PROD-STEEL-005'), ('PROD-TOBACCO-006'), ('PROD-TEXTILE-007'), ('PROD-VANILLA-008'),
('PROD-FISH-009'), ('PROD-TECH-010'), ('PROD-MINING-011'), ('PROD-AGRI-012'),
('PROD-SPICE-013'), ('PROD-WOOD-014'), ('PROD-OIL-015'), ('PROD-GAS-016'),
('PROD-SUGAR-017'), ('PROD-COTTON-018'), ('PROD-COCOA-019'), ('PROD-GRAIN-020');

WHILE @i <= 1200
BEGIN
    -- Pick a random customer
    SELECT TOP 1 @CustomerId = Id FROM @Customers ORDER BY NEWID();
    
    -- Get a valid currency for that customer's country
    SELECT TOP 1 @Currency = cc.CurrencyCode 
    FROM @Customers c 
    INNER JOIN @CountryCurrency cc ON cc.CountryCode = c.CountryCode
    WHERE c.Id = @CustomerId
    ORDER BY NEWID();

    -- Random status distribution: 40% Fulfilled, 25% Paid, 25% Pending, 10% Cancelled
    SET @Status = CASE 
        WHEN @i % 10 < 4 THEN 'Fulfilled'
        WHEN @i % 10 < 6 THEN 'Paid'
        WHEN @i % 10 < 9 THEN 'Pending'
        ELSE 'Cancelled'
    END;

    SET @OrderDate = DATEADD(DAY, -ABS(CHECKSUM(NEWID())) % 90, GETUTCDATE());
    SET @OrderId = NEWID();
    SET @NumItems = (ABS(CHECKSUM(NEWID())) % 5) + 1;  -- 1-5 line items
    SET @TotalAmount = 0;

    -- Insert Order (TotalAmount updated after line items)
    INSERT INTO Orders (Id, CustomerId, Status, CurrencyCode, TotalAmount, CreatedAtUtc, UpdatedAtUtc)
    VALUES (@OrderId, @CustomerId, @Status, @Currency, 0, @OrderDate,
            CASE WHEN @Status <> 'Pending' THEN DATEADD(HOUR, ABS(CHECKSUM(NEWID())) % 72, @OrderDate) ELSE NULL END);

    -- Insert Line Items
    SET @j = 1;
    WHILE @j <= @NumItems
    BEGIN
        SET @Quantity = (ABS(CHECKSUM(NEWID())) % 20) + 1;  -- 1-20
        SET @UnitPrice = ROUND((ABS(CHECKSUM(NEWID())) % 50000) / 100.0 + 10.00, 2);  -- 10.00 - 510.00

        DECLARE @Sku NVARCHAR(50);
        SELECT TOP 1 @Sku = Sku FROM @Products ORDER BY NEWID();

        INSERT INTO OrderLineItems (Id, OrderId, ProductSku, Quantity, UnitPrice)
        VALUES (NEWID(), @OrderId, @Sku, @Quantity, @UnitPrice);

        SET @TotalAmount = @TotalAmount + (@Quantity * @UnitPrice);
        SET @j = @j + 1;
    END;

    -- Update order total
    UPDATE Orders SET TotalAmount = @TotalAmount WHERE Id = @OrderId;

    SET @i = @i + 1;
END;

-- 4. Create some OutboxMessages for recent pending orders
INSERT INTO OutboxMessages (Id, [Type], Payload, CreatedAtUtc)
SELECT TOP 50
    NEWID(),
    'OrderCreated',
    CONCAT('{"orderId":"', CAST(o.Id AS NVARCHAR(36)), '","customerId":"', CAST(o.CustomerId AS NVARCHAR(36)), '","total":', o.TotalAmount, '}'),
    o.CreatedAtUtc
FROM Orders o
WHERE o.Status = 'Pending'
ORDER BY o.CreatedAtUtc DESC;

COMMIT TRANSACTION;

-- 5. Summary
SELECT 'Seed Complete' AS Status,
    (SELECT COUNT(*) FROM Customers) AS Customers,
    (SELECT COUNT(*) FROM Orders) AS Orders,
    (SELECT COUNT(*) FROM OrderLineItems) AS LineItems,
    (SELECT COUNT(*) FROM OutboxMessages) AS OutboxMessages;

SELECT CountryCode, COUNT(*) AS OrderCount
FROM Orders o
INNER JOIN Customers c ON c.Id = o.CustomerId
GROUP BY CountryCode
ORDER BY OrderCount DESC;

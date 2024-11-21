--建立User資料表
CREATE TABLE [User](
	user_id VARCHAR(64) PRIMARY KEY,		--使用SHA-256生成唯一id
	username VARCHAR(255) NOT NULL,			--用戶名稱
	gender CHAR(1) NOT NULL,				--性別 M/F
	birth DATE NOT NULL,					--生日
	phone_number VARCHAR(15) NOT NULL,		--電話
	email VARCHAR(255) NOT NULL,			--電子郵件
	create_at DATETIME DEFAULT GETDATE(),	--資料創建時間(自動生成)
	update_at DATETIME DEFAULT GETDATE()	--資料更新時間(自動生成)
);

--建立Admin資料表
CREATE TABLE Admin(
	admin_id UNIQUEIDENTIFIER PRIMARY KEY,					--由SQL SERVER的Guid自動生成
	admin_account INT IDENTITY(1000,1) NOT NULL,		--管理員帳號，由1000自動遞增1
	password VARCHAR(255) NOT NULL,						--密碼
	create_at DATETIME DEFAULT GETDATE(),				--資料創建時間(自動生成)
	update_at DATETIME DEFAULT GETDATE(),				--資料更新時間(自動生成)
	user_id VARCHAR(64) NOT NULL,
	FOREIGN KEY (user_id) REFERENCES [User](user_id)	--user_id外鍵
);

--建立Order資料表(顧客訂單)
CREATE TABLE [Order](
	order_id INT IDENTITY(1,1) PRIMARY KEY,				--訂單id由1開始遞增1
	date DATETIME DEFAULT GETDATE(),					--日期由SQL SERVER自動生成
	weather_condition CHAR(10),							--天氣狀況，使用文字長度限制10(輸入範例:晴朗、陰天、雨天)
	temperature INT,									--溫度，只能存放整數!!!
	user_id VARCHAR(64) NOT NULL,
	FOREIGN KEY (user_id) REFERENCES [User](user_id)	--user_id外鍵
);

--建立Inventory資料表(紀錄庫存)
CREATE TABLE Inventory(
	inventory_id UNIQUEIDENTIFIER PRIMARY KEY,		--由C#的Guid自動生成並帶進資料庫
	quantity INT,								--庫存數量
	create_at DATETIME DEFAULT GETDATE(),		--資料創建時間(自動生成)
	update_at DATETIME DEFAULT GETDATE()		--資料更新時間(自動生成)
);

--建立Meal資料表(存放商品資訊)
CREATE TABLE Meal(
	meal_id UNIQUEIDENTIFIER PRIMARY KEY,			--由C#的Guid自動生成並帶進資料庫
	name VARCHAR(255) NOT NULL,					--商品名稱
	type VARCHAR(64) NOT NULL,					--類型
	img_path VARCHAR(255) NOT NULL,				--圖片，檔案路徑(將圖片儲存到系統檔案中，並透過路徑呼叫)
	description VARCHAR(255) NOT NULL,			--描述
	price INT NOT NULL,							--售價(整數)
	cost INT NOT NULL,							--成本(整數)
	inventory_id UNIQUEIDENTIFIER NOT NULL,
	FOREIGN KEY (inventory_id) REFERENCES [Inventory](inventory_id)		--inventory_id外鍵
);

--建立Order_Meal資料表(關聯訂單和商品資訊的中間表)
CREATE TABLE Order_Meal(
	order_meal_id INT IDENTITY(1,1) PRIMARY KEY,			--id由1開始遞增1
	quantity INT,								--訂購數量
	order_id INT,
	meal_id UNIQUEIDENTIFIER,
	FOREIGN KEY (order_id) REFERENCES [Order](order_id),	--order_id外鍵
	FOREIGN KEY (meal_id) REFERENCES Meal (meal_id)			--meal_id外鍵
);

--建立Prediction資料表(業績預測)
CREATE TABLE Prediction(
	prediction_id VARCHAR(64) PRIMARY KEY,					--由python專案回傳一個長度不超過64的ID
	date DATE NOT NULL,										--預測目標日期(YYYY-MM-DD)
	predicted_sales INT NOT NULL,							--預測商品銷售量
	weather_condition CHAR(10) NOT NULL,					--目標日期的天氣狀況，使用文字長度限制10(輸入範例:晴朗、陰天、雨天)
	temperature INT NOT NULL,								--目標日期的溫度，只能存放整數!!!
	model_version VARCHAR(32) NOT NULL,						--預測模型的版本
	create_at DATETIME DEFAULT GETDATE(),					--資料創建時間(自動生成)
	meal_id UNIQUEIDENTIFIER,						
	FOREIGN KEY (meal_id) REFERENCES [Meal](meal_id)		--meal_id外鍵(取得該商品的詳細資訊)
);

-- 建立Daily_Sales_Report資料表
CREATE TABLE Daily_Sales_Report(
    report_id INT IDENTITY(1,1) PRIMARY KEY,	--報表id由1遞增1
    total_sales INT NOT NULL,					--單一產品銷售額 (整數)
    total_quantity INT NOT NULL,				--單一產品銷售量 (整數)
    date DATE NOT NULL,							--銷售日期(YYYY-MM-DD)
    meal_id UNIQUEIDENTIFIER NOT NULL,
	FOREIGN KEY (meal_id) REFERENCES Meal(meal_id),			--meal_id外鍵
);

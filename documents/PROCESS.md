# PROCESS.md — 我的練習心得

> 一個原則：**寫「具體發生的事」，不寫感想文。**
> 貼上當時真實的 prompt、真實的數字、真實的錯誤訊息——三個月後的你（和你的同事）才用得上。

#### 使用的 agent 與模型：

Claude Code，Opus 4.8（`claude-opus-4-8`）。

---

## 通用四問

### 1. 我的任務拆解

（開工前你把任務拆成哪幾步？實際做的時候順序有變嗎？為什麼變？）

- 我其實沒想太多，就照練習順序一路做下去：先 `/init` 讓 agent 把專案讀一遍、吐一份 `CLAUDE.md`，再修三個客訴 bug（一個 bug 一個 commit、各補一個回歸測試），然後加低庫存頁，最後做小重構。
- 唯一我刻意改的地方：練習 3、4 我不讓它一次寫到底，**先叫它出計畫、我點頭才准動手**。低庫存頁一動就是六層（Controller / Service / Repository / ViewModel / View / 測試），與其等它寫完再嫌東嫌西，不如先看那份文字計畫，便宜多了。
- 練習 4 它一開始想把「批次查詢」拆成另一個 commit，我說不用，直接跟驗證抽取一起做成一次重構——反正行為不變、測試全綠就好。

### 2. AI 幫上大忙的地方

（哪件事 agent 做得又快又好？**貼上當時的提問原文**，說明為什麼這樣問有效。）

- **抓 bug 根因真的快。** 我三個 bug 都只丟症狀，它就能直接點到是哪幾行、什麼機制。像客訴 2 我根本只丟一句：

  > `Gold Member order price calculate 10% off too early`

  它馬上看出 `CreateOrderAsync` 把折扣先寫進 `UnitPriceSnapshot`，`CalculateTotal` 又對總額折一次，Gold 就變成 0.9 × 0.9 = 0.81；Silver 沒事是因為那段 line-level 分支只對 Gold 生效。**我覺得有效的關鍵**是：我給的是「具體現象＋範圍」（金額偏低、只有 Gold、Silver 正常），不是丟一句「幫我修 bug」。
- 練習 3 那個**「先計畫」的長 prompt** 也很讚，它把六層分工、N+1 風險、threshold 驗證放哪層、要補哪三個測試通通先講清楚，我核准後才寫，結果幾乎一次到位（`/Products/LowStock`，測試 33 → 36 全綠）。

### 3. AI 誤導我的地方，與我如何發現

（agent 說錯／改錯／過度自信的時刻。你靠什麼抓到——對照程式碼？頁面實測？跑測試？）

- 老實說最容易讓我鬆懈的一句就是**「測試全綠」**。它每次都這樣講，但重點是——**原本那套測試在三個 bug 都還在的時候也是全綠的啊。** 所以我後來把綠燈當「必要但不夠」：bug 一律先照指南在頁面重現，重構就自己讀 diff，絕不因為它說沒事就收工。
- 我實際怎麼抓到的：
  - 客訴 1（分頁）：我在 `/Orders` 建了一筆新單，回第一頁找不到，翻到最後一頁還一片空白——先在頁面重現才去看程式。根因是 `Skip(page * pageSize)` 把 1-based 的 page 直接乘下去（應該是 `(page-1)*pageSize`）。
  - 練習 3 做完，它自己承認「瀏覽器驗證還沒做」，因為有個舊的 web 實例卡住 DLL；**頁面實測是我自己補的**，不是它。
- 我的心得：它那些「範圍建議」（這該不該另開 commit、那算不算 scope creep）是意見，不是聖旨，要不要併一起做我自己決定就好。

### 4. 我會帶回日常工作的一招

（一個具體、可複製的做法，不要寫「要多驗證」這種口號——寫出**操作步驟**。）

**只要是跨多層的功能或重構，我一律「先計畫、後動手」：**
1. 切 Plan Mode（不然就在 prompt 直接說「先只給計畫，別動檔案」）。
2. 逼它列清楚：要動哪些檔（逐一路徑＋職責）、每層怎麼分工、邊界條件、要補哪些測試。
3. 我拿計畫對規格跟既有慣例一條一條看，把超出範圍的（「順便重構 xxx」那種）直接砍掉。
4. 點頭後才准它寫；寫完我自己逐條在頁面驗證＋跑測試，最後收一個獨立 commit。

---

## 自我驗證（做到哪個階段答哪題）

### 第一階段 — Agentic Coding

練習 1

1. ✅ 三層職責我講得出來：Web（Controller/ViewModel/View，只負責接線跟顯示）、Core（domain＋service 的商業邏輯：折扣、庫存、狀態轉移）、Infrastructure（EF Core DbContext／repository／migration／種子資料）。
2. ✅ 對照它的說法時我有抓到不精確的地方：它一度把批次查詢講成「之後另開 commit 的事」，也曾把折扣講得像逐項在套——但實際規則是**只在訂單總額折一次**。
3. ✅ 我知道商業邏輯要放 Core service，加一個新頁面要動六個地方：Controller→Service→Repository→ViewModel→View→測試。
   （補一句：`/init` 產出的 `CLAUDE.md` 現在被 `.gitignore` 擋掉、還沒進版控。）

練習 2

1. ✅ 三個 bug 我都先在頁面重現過才去翻程式。
2. ✅ 我給 agent 的是具體觀察（新單在第一頁找不到／最後一頁空白、Gold 金額偏低但 Silver 正常、取消後庫存沒加回），不是直接貼客訴。
3. ✅ 每個修完我都回頁面確認症狀真的不見了。
4. ✅ 每個 bug 補一個回歸測試，`dotnet test` 全綠（練習 2 結束是 33 個）。
5. ✅ 三個獨立 commit：`254b8dd`（分頁）、`862f706`（Gold 重複折扣）、`e80411b`（取消沒加回庫存），message 都寫成 症狀 → 根因 → 修法。
6. **思考題：為什麼原本的測試沒抓到這三個 bug？** 我後來想通了：
   - 分頁：舊測試 `GetOrders_ReportsTotalCountAndTotalPages` 只驗 `TotalCount`／`TotalPages`，**從來沒檢查某一頁到底回傳哪些列**。
   - Gold：舊 pricing 測試都是**自己 new 一個 Order、直接塞 `UnitPriceSnapshot`**，根本沒走 `CreateOrderAsync`——而雙重折扣就是在建單那條路上發生的。
   - 取消：舊 cancel 測試只驗狀態變 Cancelled，**沒驗庫存有沒有加回**。
   - 講白了就是：測試只斷言「旁邊那些淺層屬性」，還**繞過了真正的程式路徑**，所以使用者真的會看到的行為根本沒被守到。

練習 3

1. ✅ `/Products/LowStock` 不帶參數→門檻 10；`?threshold=3`→結果跟著變。
2. ✅ `?threshold=0`、`-1`→跳表單驗證錯誤（`[Range(1,9999)]`），不是 500。
3. ✅ 近 30 天售出數量有排除 Cancelled（`GetSoldQuantitiesAsync` 的 `GROUP BY` 過濾 `Status != Cancelled`）。
4. ✅ 停售商品不會出現（`IsActive && StockQuantity < threshold`）。
5. ✅ 分層跟命名沿用既有 Products 的慣例（薄 Controller、邏輯在 service、EF 查詢在 repository、View 綁 ViewModel）；售出量用一條聚合查詢＋記憶體 join，沒有 N+1。
6. ✅ 補了 3 個 service 測試（門檻過濾＋升冪＋排除停售、排除 Cancelled、排除 30 天前），`dotnet test` 36 全綠。commit `f4313d2`。

練習 4

1. ✅ 重構完 `dotnet test` 還是全綠（36 → 49）。
2. ✅ 我講得出「改了什麼、沒改什麼」：抽出 `OrderValidator` 讓驗證有個單一的家、還能單獨測；`GetByIdAsync` → `GetByIdsAsync` 批次查詢把 N+1 幹掉（建單 1+N→1+1、取消 N→1）。**沒動的**是錯誤訊息字串、檢查順序、短路 vs 收集的語意、扣／加庫存的副作用位置，還有回傳型別。
3. ✅ 我用 code review 的眼睛看過 diff，還補了 13 個測試：直接測 `OrderValidator` 每條分支、`GetByIdsAsync` 的契約、多商品建單／取消的 roundtrip。commit `28c14f2`。

---

## 附錄：值得留下的對話片段

（貼 1–2 段最有代表性的 prompt 與回應**摘要**——不用貼全文，重點是「我怎麼問」和「它怎麼答」。）

- **「先計畫」那個 prompt（練習 3）**——我把整段規格貼上去後補一句：

  > 先不要寫程式，給我一份實作計畫：要新增/修改哪些檔（逐一路徑＋職責）、每層怎麼分工、「近 30 天售出數量（排除 Cancelled）」放哪層用什麼查詢、會不會 N+1、threshold 驗證放哪層、打算補哪 3 個測試。動手前先讀 ProductsController、ProductService/IProductService、Views/Products/Index.cshtml，沿用同一套慣例。

  **它怎麼答**：先把那三個檔讀了，列出 13 個要增修的檔，說售出量用一條 `GROUP BY` 聚合＋記憶體 join 避免 N+1，threshold 用 ViewModel 的 `[Range]` + `ModelState`（沒帶預設 10、≤0 顯示表單錯誤而不是 500），還提了 3 個測試。我點頭後它才開始寫。

- **只丟症狀那個 prompt（練習 2 客訴 3）**——我就給它一句客訴：

  > 商品頁的庫存數字跟實際盤點對不上，而且好像每次退單（取消訂單）之後就更少

  **它怎麼答**：它指出 `CancelOrderAsync` 先把 `Status` 設成 `Cancelled`、才去判斷 `if (Status == Pending || Confirmed)` 要不要加回庫存——這條件因此永遠是 false，加庫存那段根本是從沒跑到的死碼。修法是**先加回庫存、再改狀態**，順便補一個「建單扣庫存、取消後加回」的回歸測試。

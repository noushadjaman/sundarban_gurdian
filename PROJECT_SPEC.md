# সুন্দরবন রক্ষা (Sundarban Guardian) — Master Project Spec

> এই ফাইলটা Antigravity-কে পুরো প্রজেক্টের reference হিসেবে দাও। এটা এমনভাবে লেখা যাতে ও ধাপে ধাপে কাজ করে, প্রতিটা বড় ধাপ শেষে আমাকে জানায়, এবং কোনো real-world সংখ্যা (satellite reading) দরকার হলে নিজে থেকে বানিয়ে না নিয়ে আমাকে জিজ্ঞেস করে।

---

## 0. Reporting Protocol (এই নিয়মটা সবচেয়ে গুরুত্বপূর্ণ, সব কাজের আগে পড়ো)

- প্রতিটা numbered task শেষ করার পর, **plain বাংলা/ইংরেজিতে** আমাকে বলো ঠিক কী করলে, কোন ফাইল/GameObject বদলেছে, আর `read_console` দিয়ে error আছে কিনা confirm করো।
- **কোনো real-world data value (satellite reading, coordinate, date) লাগলে নিজে থেকে অনুমান করে বসিয়ে দিও না** — placeholder রেখে আমাকে স্পষ্ট করে জিজ্ঞেস করো: "এই zone-এর জন্য real NDVI/salinity value কত, বা কোন coordinate ব্যবহার করবো?"
- ইতিমধ্যে যা আছে তা মুছে না ফেলে তার উপর build করো — Deer/Tiger GameObject, LandAnimalController, DolphinSwimController, HilshaMigrationController, LevelLoader, level JSON ফাইলগুলো আগে থেকেই আছে।
- একবারে সব কাজ শেষ করার চেষ্টা কোরো না — একটা task শেষ করে report করে, তারপর পরের task শুরু করো।

---

## 1. Game Overview

একটা 3D wildlife-rescue game যেখানে player একেক লেভেলে একেকটা বিপন্ন সুন্দরবন প্রজাতি হয়ে খেলে, আর প্রতিটা লেভেলের hazard/danger আসে **real NASA satellite data** থেকে — hardcoded না।

**Species ও তাদের core mechanic:**
| প্রজাতি | Mechanic | Data source |
|---|---|---|
| চিত্রা হরিণ (Deer) | Land avoidance — hazard zone এড়িয়ে চলা | MODIS NDVI (vegetation loss) |
| রয়েল বেঙ্গল টাইগার (Tiger) | একই mechanic, ভারী/ধীর movement | MODIS NDVI |
| গাঙ্গেয় শুশুক (Gangetic Dolphin) | নদীতে সাঁতার + surfacing mechanic | Land Surface Temp (proxy) / salinity |
| ইলিশ (Hilsha) | বঙ্গোপসাগর থেকে মেঘনা-পদ্মা migration corridor | Salinity + temperature gradient |

---

## 2. Current Progress (এগুলো আগে থেকেই আছে, নতুন করে বানানোর দরকার নেই)

- Unity 6.6 URP project, MCP for Unity bridge connected এবং কাজ করছে
- Scripts: `LevelData.cs`, `LevelLoader.cs`, `HazardZoneTrigger.cs`, `HudEvents.cs`, `SpeciesHealth.cs`, `LandAnimalController.cs`, `DolphinSwimController.cs`, `HilshaMigrationController.cs`
- Deer GameObject আংশিক সেটআপ (CharacterController, Animator, LandAnimalController)
- `level_tiger_01.json`, `level_hilsha_bongoposagor_to_meghna.json` placeholder ডেটা সহ আছে
- **Real NASA data ইতিমধ্যে হাতে আছে**: AppEEARS থেকে NDVI (MOD13Q1) আর Land Surface Temperature (MOD11A2) এর CSV — ৯টা coordinate কভার করে (tiger_katka, deer_kotka, dolphin_chandpur, hilsha_point1 থেকে 6)
- **Salinity data এখনো নেই** — এটা AppEEARS-এ পাওয়া যায়নি (land data archive, ocean salinity না); PO.DAAC থেকে আনতে হবে অথবা proxy narrative ব্যবহার করতে হবে — এই সিদ্ধান্ত এখনো নেওয়া হয়নি, আমাকে জিজ্ঞেস করবে কোন পথে যাবে তার আগে salinity-নির্ভর কিছু চূড়ান্ত করার আগে

---

## 3. Remaining Features To Build

### 3.1 Species setup (বাকি থাকা)
- Tiger, Gangetic Dolphin, Hilsha GameObject সেটআপ (আগের কথোপকথনে বিস্তারিত prompt দেওয়া আছে — component, Animator Controller ইত্যাদি)
- প্রতিটার জন্য placeholder capsule/primitive দিয়ে movement mechanic আগে confirm করা, তারপর real model বসানো

### 3.2 Real NASA data integration
- AppEEARS CSV (NDVI + LST) parse করে `level_tiger_01.json`, `level_deer_01.json` (নতুন বানাতে হবে), আর Hilsha corridor JSON-এ বসানো
- MODIS LST Kelvin থেকে Celsius conversion (`value * 0.02 - 273.15`) মনে রাখতে হবে
- প্রতিটা JSON-এর `dataSource` field-এ real product name + date বসাতে হবে (placeholder text না)

### 3.3 Visual style — REAL SUNDARBAN LOOK (গুরুত্বপূর্ণ)
- এই গেম **stylized/cartoon না**, বরং যতটা সম্ভব **বাস্তব সুন্দরবনের মতো দেখতে** environment চাই — ঘন mangrove tree line, ঘোলাটে বাদামি-সবুজ নদীর পানি, নিচু জলাভূমি টেরেইন
- প্রাণীদের model **realistic/recognizable** হতে হবে, প্রতিটা প্রজাতি স্পষ্টভাবে চেনা যায় এমন (cartoon-blocky না) — যদি free CC0 realistic model না পাওয়া যায়, তাহলে stylized কিন্তু anatomically সঠিক silhouette ব্যবহার করতে বলবে, আর আমাকে জানাবে কোন species-এর জন্য ভালো model খুঁজে পায়নি
- Reference: Kenney Nature Kit (environment), Quaternius Fish Pack (dolphin/fish base), Sketchfab CC0 filter (tiger/deer/specific models) — এগুলো আগে discuss করা হয়েছে

### 3.4 UI — Top-left minimap (REAL MAP ব্যবহার করলে ভালো)
- স্ক্রিনের উপরের-বামে একটা minimap থাকবে যেখানে player-এর বর্তমান অবস্থান দেখা যাবে
- **NASA competition-এর judging-এর জন্য এটা আরও শক্তিশালী হবে যদি minimap-এ আসল satellite/topographic imagery ব্যবহার করা হয়**, generic fog-of-war minimap না
- Approach: প্রতিটা level-এর real coordinate অনুযায়ী একটা static satellite image (NASA Worldview true-color composite, বা Google/Mapbox static map export — লাইসেন্স চেক করে নিতে হবে) background হিসেবে ব্যবহার করে, তার উপর player position আনুপাতিকভাবে (lat/lon থেকে normalized x/y) একটা marker বসিয়ে দেওয়া
- এটা তৈরি করার আগে Antigravity আমাকে জিজ্ঞেস করবে: কোন image source ব্যবহার করা হবে (NASA Worldview screenshot manually নেওয়া, নাকি অন্য কিছু), কারণ এটার জন্য আমাকে actual image file জোগাড় করে দিতে হবে — এটা code দিয়ে solve করা যাবে না
- Minimap UI Unity-র UI Toolkit বা uGUI দিয়ে বানানো যেতে পারে — RawImage/Texture2D দিয়ে satellite image দেখানো + একটা simple dot/icon marker

### 3.5 HUD elements
- Health bar (species-নির্ভর)
- Hazard zone-এ ঢুকলে real data value popup (আগে থেকে `HudEvents.cs` এই কাজের জন্য আছে, শুধু UI display বানাতে হবে)
- Level-শেষে fact card (Bengali) — real data + NASA source citation দেখানো

### 3.6 Game flow / Manager
- GameManager (আগের prompt-এ discuss করা) — level load, win/lose condition
- Level select screen (কোন প্রাণী দিয়ে শুরু করবে)

---

## 4. Explicit Data Needs Checklist (Antigravity এই তালিকা ধরে ধরে আমাকে জিজ্ঞেস করবে)

- [ ] Salinity data source সিদ্ধান্ত (PO.DAAC / proxy narrative / government report) — **এখনো ঠিক হয়নি**
- [ ] Minimap-এর জন্য satellite image source এবং actual image file — **এখনো নেই**
- [ ] Realistic animal 3D model (Tiger, Deer, Dolphin, Hilsha) — **এখনো ঠিক হয়নি কোন pack ব্যবহার হবে**
- [ ] প্রতিটা species-এর জন্য চূড়ান্ত real coordinate নিশ্চিতকরণ (আগে দেওয়া তালিকা অনুযায়ী, বদলাতে চাইলে জানাবো)

---

## 5. Immediate Next Task (Antigravity এখন এটা দিয়ে শুরু করবে)

1. `read_console` দিয়ে বর্তমান project অবস্থা/error চেক করো
2. উপরের ৩.১ (Tiger/Dolphin/Hilsha GameObject setup) সম্পূর্ণ করো, একটার পর একটা, প্রতিটার পর report করো
3. তারপর ৩.২ (real NASA data JSON-এ বসানো) — আমি CSV ফাইল attach করে দেব
4. এরপর ৪ নং তালিকার open questions নিয়ে আমাকে জিজ্ঞেস করো, ধরে নিয়ে এগিয়ে যেও না

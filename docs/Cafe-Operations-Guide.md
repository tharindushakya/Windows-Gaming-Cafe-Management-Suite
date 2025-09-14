# Windows Gaming Café – Salesperson-Facing Operations Guide & Implementation Plan

This document summarizes the desired end-to-end customer journey and outlines how to implement it across the existing solution (API, Admin, POS, Data/Core). It’s tailored for a salesperson/operator workflow (not self-service kiosk).

## Objectives

- Smooth membership onboarding and login
- Station selection with card tap/unlock and timed game sessions
- POS purchases tied to the customer account with loyalty accrual
- Pause/resume session timer based on actual gameplay/activity
- Finalize payment (cash/card/wallet) with consolidated charges (time + items)
- Persistent station client that runs in the background and prevents unauthorized close
- Customer profile visibility (time history, orders, payment methods)

## Roles & Systems

- Customer: member with email/password and/or card/badge (NFC/QR/magstripe)
- Staff/Cashier: operates POS to assist with sales and customer linkage
- Station Client Agent: per-PC background app/service managing lock/unlock and session timer
- API: central business logic, auth, session control, loyalty, payments, reporting
- Admin UI (Blazor/React): management console for inventory, pricing, stations, users
- POS (WPF): sales and quick operations (tie sales to customer, take payments)

## End-to-End Flow (Happy Path)

1) Membership

   - Customer registers (email/password) and is issued a card/badge mapped to their UserId.

2) Sit & Unlock

   - Customer chooses a station. Taps card on reader at the station.
   - Station Client Agent calls API → validates card → unlocks station (Windows session or in-app overlay) and associates “Active Session”.

3) Start Session

   - When a supported game/app launches (or station becomes active), Agent starts session timer.
   - Pause timer on OS lock/idle or when no whitelisted game is active; resume when active again.

4) Purchases During Session

   - At food stall, staff uses POS to search/select the customer (by card or email/ID) and adds items. POS posts transactions linked to UserId.
   - Loyalty points accrue per configured rules.

5) End & Payment

   - Customer ends play or taps out. Agent stops the timer and posts final usage.
   - POS shows consolidated dues: session time charge + items − discounts/loyalty.
   - Staff takes payment (cash/card/wallet). Transaction status updates to Completed.

6) Customer Records

   - Customer can view their session history, orders, payments in their profile.

## Functional Requirements Checklist

- Membership & Identity
  - Email/password auth + card/badge mapping to UserId.
  - Staff can create new members quickly from POS/Admin.
- Station Management & Sessions
  - Station registry (name, PC/console, status).
  - Start/pause/resume/stop sessions; capture timestamps and billable seconds.
  - Agent enforces kiosk/lock overlay when not authorized; cannot be closed by customer.
- Game Activity Detection
  - Whitelist of games/processes; timer follows foreground active state.
  - Idle/lock detection to auto-pause.
- POS Sales & Loyalty
  - Add items to cart, tie to customer; inventory decrements and transaction entries created.
  - Loyalty accrual and redemption rules.
- Payments
  - Cash/Card/Wallet; ability to reconcile session charges and items.
- Reporting & Profiles
  - Daily reports; per-user history of sessions, orders, payments; loyalty balance.

## Architecture Overview (Mapping to Solution)

- Core/Data (existing)
  - Users, Products, Transactions, InventoryMovements, Wallets, (add) Stations, Sessions
- API (existing)
  - Endpoints for auth, users, products, transactions, inventory, loyalty, stations/sessions
- POS (existing WPF)
  - Customer selection, cart, payment, links transactions to UserId
  - Add: quick “Link Card” and “Charge Session” integration
- Admin (existing)
  - Configure inventory, station catalog, pricing, tax, loyalty rules
- Station Client Agent (new)
  - Windows service/tray app per station; communicates with API; enforces lock/unlock; tracks game activity and session timer

## Data Model Touchpoints (Add/Confirm)

- Station
  - StationId, Name, MachineId, Status (Available/InUse/Locked), LastHeartbeat, CurrentSessionId
- GameSession
  - SessionId, UserId, StationId, StartTime, EndTime, TotalSeconds, BillingRateId, Status (Active/Paused/Ended)
- CardBinding
  - UserId, CardToken (NFC/QR), LastUsed, IsActive
- Pricing & Loyalty
  - BillingRate (hourly/step pricing), LoyaltyRule (earn/redeem), LoyaltyLedger linked to UserId

Note: Transactions already include Type, Amount, PaymentMethod, Status; reuse for items and session charges.

## Station Client Agent (Design Notes)

- Responsibilities
  - Kiosk mode / lock overlay when not authorized
  - On card tap → call API to authorize and start session
  - Detect game activity (process whitelist), OS lock/idle → pause/resume
  - Heartbeat and log upload; graceful stop at end session
- Tech options
  - .NET WPF/WinUI app with autostart + watchdog service
  - Windows APIs: user idle detection, session lock/unlock events, foreground process polling
  - Optional: third-party card reader SDK (NFC/magstripe)

## Payments & Card Reader

- POS continues to record PaymentMethod and Transaction; integrate a payment gateway later (Stripe Terminal/Adyen/Local bank SDK) behind an abstraction.
- Station Agent can remain out-of-scope for payments; checkout happens with staff at POS.

## Loyalty and Wallet

- Accrue points per spend/time; allow redemption in POS.
- Wallet top-up/usage tracked in Transactions.

## Security & Ops

- Least-privilege API keys for Station Agent
- Audit logs for session start/stop and cash adjustments
- App auto-restart on crash; agent watchdog
- Encrypt card token; never store raw PAN (use tokenized card/ID only)

## Implementation Phases

- Phase 1 (MVP)
  - POS: Ensure customer selection, item sales, daily report, loyalty accrual basics
  - API/Data: Add Station, GameSession, CardBinding models + basic endpoints
  - Agent: Minimal app to lock/unlock via API, start/stop sessions manually from staff
- Phase 2
  - Agent: Process whitelist detection, idle/lock detection, auto pause/resume
  - POS: Consolidated checkout that pulls active session dues + items in one payment
  - Loyalty: Earn on time and purchases; redemption flow in POS
- Phase 3
  - Payments: Integrate card reader/gateway abstraction
  - Profiles: Customer UI (view history, payments, methods) via Admin/Portal
  - Hardening: Kiosk mode enforcement, watchdog, telemetry, reports

## Tasks Mapped to Repos

- Core/Data
  - Add entities: Station, GameSession, CardBinding, BillingRate, LoyaltyRule, LoyaltyLedger
  - Migrations and relations; update DbContext
- API
  - Endpoints: stations (CRUD, heartbeat), sessions (start/pause/resume/stop), card-binding (lookup), loyalty (earn/redeem)
  - DTOs + validation; auth for station clients
- POS
  - Add Select Customer button (wired), show selected customer
  - Add “Charge Session” panel to fetch active session dues; consolidate with cart on checkout
  - Scan card to select customer (if reader attached to POS)
- Admin
  - Manage stations, rates, loyalty rules; card issuance tool
- Agent (new project)
  - Windows app/service with API client, process detection, lock overlay, autostart

## Success Criteria

- Staff can run full flow without manual time tracking
- Sessions auto pause/resume with game activity
- One consolidated checkout per customer
- Inventory and loyalty are correct
- Station remains controlled and recoverable

## Open Questions

- Which card reader SDK/hardware?
- Windows domain vs. in-app overlay for lock/unlock?
- Exact pricing rules (per minute, rounding, caps)?
- Loyalty earn/redeem rates and exclusions?

---

Last updated: 2025-09-12

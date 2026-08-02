# Domus — Product Overview

Domus is a household management platform centered around a shared House.

The House is the primary domain context.

## Foundation

A House has multiple Members.

Each Member has a role within the House:

* Admin
* Member

A Member may have multiple authenticated devices.

Admins govern the House and may:

* approve or reject expenses;
* create and assign tasks;
* approve or reject task completion;
* manage household operations.

Membership and permissions belong to the Domus domain, not to the Identity Provider.

## Expenses

Domus supports different expense lifecycles.

### Recurring expenses

Examples: rent, electricity, subscriptions.

Recurring expenses are approved when created and generate future occurrences automatically.

### Planned expenses

Examples: maintenance, repairs, renovations.

A Member requests the expense.

An Admin is notified and approves or rejects it before execution.

### Urgent expenses

Examples: emergency plumbing or electrical repairs.

The expense can be recorded immediately.

Admins are notified, but prior approval is not required.

All expense types contribute to the household financial balance.

## Tasks

Admins may create household tasks.

Tasks can be assigned:

* permanently to one Member; or
* through rotation between Members.

When a task is assigned, the responsible Member is notified.

The Member marks the task as completed.

An Admin then:

* accepts the completion; or
* rejects it and optionally reschedules the task.

Rejected or rescheduled tasks return to the execution flow.

Tasks may be recurring.

For rotating recurring tasks, each occurrence is assigned to the next Member in the rotation.

## Calendar

The household has a shared calendar.

Calendar events may notify Members.

The calendar may also surface dates originating from other capabilities, such as:

* recurring expense due dates;
* scheduled tasks;
* approved planned expenses.

The Calendar should consume domain events or projections rather than becoming the owner of those concepts.

## Financial Balance

The financial balance provides a household-level financial view.

It receives financial entries originating from all expense types.

It should support monthly views such as:

* income and expenses;
* categories;
* expenses by Member.

The balance is a financial projection of household activity rather than the owner of expense workflows.

## Notifications

Notifications are a cross-cutting capability.

A notification may be delivered to all authenticated devices belonging to the target Member.

Examples of notification triggers include:

* expense approval requested;
* urgent expense registered;
* task assigned;
* task completed;
* task rejected or rescheduled;
* household calendar event.

Domain capabilities decide when something relevant happened.

The notification capability decides how that information is delivered.

## Receipts

Domus may ingest purchase receipts.

Preferred ingestion:

1. QR code / NFC-e structured data;
2. image extraction when structured data is unavailable.

Receipt information may include:

* items;
* prices;
* merchant;
* purchase date.

Receipt data contributes to the historical context of the House.

## Intelligence

The Intelligence capability consumes household historical context.

Potential responsibilities include:

* normalizing product descriptions;
* categorizing purchased items;
* identifying recurring purchase patterns;
* forecasting likely future expenses;
* suggesting opportunities for savings.

Intelligence should consume existing household data and must not become the source of truth for transactional domain data.

AI-generated conclusions should remain distinguishable from authoritative household records.

## Domain Direction

Expected major capabilities are:

* Users
* Houses and Membership
* Expenses
* Tasks
* Calendar
* Financial Balance
* Notifications
* Receipts
* Intelligence

This list represents product direction, not a requirement to implement all capabilities upfront.

Capabilities should be introduced incrementally as concrete product requirements emerge.

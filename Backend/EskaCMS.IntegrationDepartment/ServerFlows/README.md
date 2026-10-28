# Server flows

A server flow is an `apiGroupList` (the same JSON the dynamic data provider modal saves) that the
**server** runs on its own. It keeps going if the customer closes the page, survives restarts,
never sends a policy twice, retries what is safe to retry, and asks a person when it cannot decide.
The calls are written in the pages' own request editor, and each setting means on the server what
it means on a page.

What starts a run (`settings.trigger.type`):

| trigger | starts when | reads | who reads the answer |
|---|---|---|---|
| `orderPaid` | an order is paid by a verified payment (`paymentMethods`, `"*"` = any gateway); other paid orders wait for an admin; orders paid by one of `skipPaymentMethods` (e.g. `["SADAD"]`) never start it | `fromOrder`, the cart, the order's customer | the payment result page (`waitSec`) |
| `page` | a page asks (`POST api/ServerFlows/run`), for its signed-in visitor (`who`), once per journey / customer / input value (`once`, `onceInput`) | `fromInput` (only the inputs the flow reads, checked by `inputs`), the visitor, their cart | the page (`waitSec`) |
| `schedule` | every N minutes or at times of day, in the flow's time zone (`everyMinutes` / `times`, `days`) | fixed values, today's date, earlier results | nobody |
| `webhook` | another system calls `POST api/ServerFlows/hook/{siteId}/{flow}`, signed with its private key (`auth: "signature"`; the flow keeps the public keys) or with the flow's key (`X-Flow-Key`; the flow keeps its SHA-256, `keyHash`); see [Calls from other systems](#calls-from-other-systems-webhook) | `fromInput` (what it sends) | the caller (`waitSec`) |
| `orderStatus` | an order moves into one of `toStatuses` (not the paid ones) | as `orderPaid` | nobody |

A trigger only starts runs for what happened since the flow (by its name) has been on. A new
version keeps that moment only when it replaced the one before at once and starts exactly the same
way (the same trigger with the same settings and time zone); otherwise it counts from its own
switch-on (a product, status or time only the new version has is never reached back for).

## How it runs

```
trigger (payment, page, schedule, webhook, order status)
        |
ServerFlowTriggers / PageRuns / Schedules / Webhooks / OrderStatuses: one run per RunKey
        |                       (unverified payment -> AwaitingApproval)
ServerFlowWorker (BackgroundService) -> ServerFlowRunner
        lease, step rows, retries, guard, outputs, amount check
        |
Succeeded (Reference) | Waiting (retry, polling) | NeedsAttention (+ e-mail)
        |
pages and callers read it: POST api/ServerFlows/result, GET api/ServerFlows/run/{id}?wait=
```

- **Nothing lives only in memory.** Runs and their steps are rows. The worker sweeps every
  `SweepSeconds` for due runs, runs whose worker died (expired lease), payments that missed their
  trigger, scheduled times and order status moves. The history sweeps read on from where the last
  one got to (by row id), never the whole history.
- **One worker at a time per run.** A run is claimed with one conditional UPDATE (the lease); every
  later write re-checks the owner and that the run is still running. The lease is renewed while a
  call is out.
- **A "Calling" row is written before every call.** After a crash it tells the next worker the call
  may have gone out.
- **Calls run as the order's customer** (a page flow: the visitor; schedule and webhook: the site),
  in their own DI scope marked as a server scope, with the site, user, culture and cart as request
  headers.
- **What is kept is masked.** Each attempt keeps the body it sent and the answer it got for the
  Admin: secrets (keys such as password, token, otpCode, cvv2, x-api-key, cookie, also lists) and
  the flow's `privacy.hide` values are `***`. Error texts, the run's reason, the log and the e-mails
  are built from that masked answer.

## The call's settings

Every data source call says what it does (`server.class`; there is no default on purpose):

| class | on an error or timeout |
|---|---|
| `read` | retried (default 3 times, after 10 s, 1 min, 5 min) |
| `idempotentWrite` | retried the same way (sending twice has the same effect) |
| `nonIdempotent` | **never re-sent blindly.** The guard (`server.guard`) asks the other side; "not done" allows one more send; anything else waits for a person |

The rest is on the call's own screens, as on pages:

- **Timeout (ms)** (Request): at most 120000; ignored (and said) for a `nonIdempotent` call and for
  a DCMS operation, which are always waited for to the end.
- **Retry attempts / Retry Delay (ms)** (On error): at most 10 tries and 1 hour; not used by a
  `nonIdempotent` call. 0 is "not set" (the server's retries).
- **Keep going on error** (On error): the run goes on without the call; what it answers instead is
  its fallback (`fallbackValueMapping` / `fallbackValue`), else `{ error, name }`.
- **Error message**, the other side's words at `responseMessagePath`, **Translate the message**
  (key, prefix, message patterns, values): what the page or caller reads when the call stops the
  run, translated from the site's Translations; otherwise the site's sentence `ServerFlows.failed`.
  A customer never reads another system's raw error.
- A response check (`responseErrorChecksConfig`) that matches is a refusal: not retried.
- A group's own On error settings are not used on the server (it says so); its response checks are.

`server.timeoutSec`, `server.retry`, `server.critical` and `server.refusedMessage` are the older
spellings: still read, and pointed out with their new place.

`server.outputs` declares what a call keeps (`{ "policyNo": "Policy.PolicyNo" }` or `{ "path",
"valueOps", "public": true }`); only declared outputs feed later calls, and `public` ones are what
a page or caller reads back. `server.authToken` names a token under `ServerFlows:Tokens`.
`server.sampleResponse` is the answer Try it simulates with.

## What a flow may use

**Values (`typeOfValue`)**: `static`, `null`, `currentDate`, `fromOrder`, `fromCart`,
`fromAdditionalDetailsCart`, `fromInput`, the signed-in user as pages read it (`loggedInUser` with a
path such as `username`, `userOption` with an option's name, `currentUsername`), `previousResults`
(`request.output`, also `group.request.output` and `request.list[0].x`), `objectOfObject`,
`arrayOfValues`, and in a loop `currentItem`, `currentItemKey`, `currentIndex`. After each value:
`defaultValue`, then `valueOps`, then `transform`, then `required`.

**Conditions** (`executeIf`, conditional mappings) read `apiRes` / `previousApi` (earlier
results), `fromOrder`, the cart, `fromInput`, `userInfo` / `userOptions` and literals. Response
checks and guard decisions read the answer itself. Statements: `C1`, parentheses and lowercase
`and` / `or` / `not` only. `fromUser` is the older name of the user; still read.

**Also supported**: JavaScript transforms (in a sandbox, as `new Function` in the browser),
`transformResponse` valueOps / script / pick / omit, `readResFrom`, group `transformResult`,
`loopOn` and conditional groups (one step per item), parallel groups (run in order), repeat /
polling (`repeatConfig`). **Not**: file answers, form data, `opsRef`, `transformPrevResults`, local
endpoints other than the three below.

**DCMS operations** (`"type": "local"`, run in-process, safe to repeat):

| endpoint | body | answer |
|---|---|---|
| `/Notification/send` | what the pages send to it | the notifier's result |
| `/Account/EnsureUser` | `preferUserId, userName, lookupOption, lookupValue, email, phoneNumber, fullName, dateOfBirth, roleId, groupId, status, userOptions[], welcomeTemplateId, reportOptions[]` | `{ userId, userName, created, matchedBy, options }` |
| `/Account/AddUpdateUserOptions` | `userId, userOptions[{ PropertyName, Value, IsVisible, IsEditable }]` | `{ userId, added, updated, skipped }` |
| `/Order/ChangeStatus` | `orderId` or `lookupPath, lookupValue`; `paymentMethod, fromStatus, toStatus, amount, tolerance, note` | `{ orderId, from, to, total, changed }` |

`/Order/ChangeStatus` moves one order with DCMS's own status change (`IOrderService.OrderChangeStatus`,
as the Admin and the gateways do it). The order is found by its id, or by `lookupValue` at
`lookupPath` in its items' additional details (e.g. `payment.sadadNo`); it must be in `fromStatus`
and, when given, placed with `paymentMethod`. A paid status (`PaymentReceived`, `PaidByCoupon`) needs
`amount`, which must be the order's total within `tolerance` (0.01). An order already in `toStatus`
answers `changed: false`. `fromStatus`, `toStatus`, `paymentMethod` and `lookupPath` must be fixed
(Static) values, and a flow a page starts cannot use it (`ORDER_FROM_PAGE`). Example:
`Frontend/config-exports/server-flow-examples/sadad-paid` (another system says a SADAD bill is paid).

A run without a user (a webhook's, a schedule's) calls data sources and these operations as the
site's "Server flows" guest account (`server-flows.site{id}@guest.eskacms.com`), made the first time
it is needed: DCMS writes the calling user on every data source call, and user 0 does not exist.

The account operations touch customers' accounts only (never admin or integration accounts). EnsureUser finds the
account by the order's own account when it is the same person's, else the user name, else the user
option `lookupOption` = `lookupValue`; never by e-mail. Only then does it call DCMS `Register`, with
`roleId` a fixed value of the flow (never an admin role), and with `welcomeTemplateId` it e-mails
the user name and a generated password (never stored or logged). In a flow a page starts, whose
account it is never comes "From the page".

**Settings**: `paymentCheck` (`amountFrom`, `compareTo`, `tolerance`), `outputs.orderReference`
(the run's reference; else the first public value of a call that answers once), `notify.onAttention`
(`emails`, `template`: an Email templates id), `timeZone`, `privacy` (`hide`: value names shown and
kept as `***`; `keepRunsDays`).

**Schedules and summer time**: times are counted in the flow's time zone. A time the clock skips
when it goes forward does not run that day; when it goes back, a time in the repeated hour runs
once (at its second showing), so `everyMinutes` leaves a gap one hour longer across the change.
Run keys and times are written with the invariant culture, whatever the server's culture. (Before
2026-10-15 a schedule's key was written in the server's culture, Hijri on ar-SA; the sweep checks
that older key too, so the latest time is not run again after the update.)

## Calls from other systems (webhook)

How the other system proves a call is its own (`settings.trigger.auth`):

- `"signature"` (signed requests; the Admin's default for new flows): the other system signs every
  request with its **private** key; the flow keeps only the **public** key(s) (`publicKeys`, at most
  3: the one in use and its replacement). Nothing secret is sent or stored: a copy of the flow, the
  database, a log or a captured request cannot make a new call. The method, path, time and body are
  signed, so a request cannot be changed, sent to another flow, or used again after the time allowed.
- `"key"` (the older way, and the default when `auth` is missing): the flow's key in `X-Flow-Key`.
  Anyone who gets the key can call until a new key is made; the Admin warns (`KEY_FOR_WRITES`)
  when such a flow changes data.

A signed request carries two headers:

| header | value |
|---|---|
| `X-Flow-Timestamp` | the Unix time in seconds when it was signed (`ServerFlows:SignatureMaxAgeSeconds`, default 300, either way of the server's clock) |
| `X-Flow-Signature` | base64 (or base64url) of the signature of the text below |

The text signed (UTF-8, five lines joined with `\n`, no line break at the end):

```
DCMS-FLOW-SIGNATURE-V1
1792650000
POST
/api/ServerFlows/hook/6/sadad-paid
{SHA-256 of the body's bytes exactly as sent, 64 lower-case hex digits; GET: of nothing}
```

The path is from `/api/` on, without the query (`/run/{runId}` for reading a run). Keys: ECDSA
P-256 with SHA-256 (the signature as DER or as r|s, 64 bytes), or RSA of 2048 to 8192 bits with
SHA-256 (PKCS#1 v1.5 or PSS). The Admin makes a P-256 key pair for the other system (the private
key is shown once and kept nowhere), or reads their public key (PEM, `RSA PUBLIC KEY`, a
certificate, a JWK or base64; a pasted private key is refused). `WebhookAuth` holds all of it as
pure functions.

The same signed request sent again (a retry, or a copy within the time allowed) answers about the
run it started (run key `webhook:{site}:{flow}:request:{hash of the text signed}`); a flow that
runs once per value (`once: "input"`) keys on the value first.

Moving a system over: a signed flow still takes its old key through `keyUntil` (`yyyy-MM-dd`, the
flow's time zone; at most 90 days ahead, `KEY_UNTIL_FAR`). After that day the key is refused with
"This flow takes signed requests only".

Addresses: `allowedIps` (`["10.20.30.40", "10.20.31.0/24"]`, at most 50; IPv4 written as four
numbers; nothing wider than /8, or /16 for IPv6) takes calls from those addresses only; an empty
list takes any. The caller's address is the one the request came from (after the forwarded-headers
set-up's hop), or, when that is one of Cloudflare's published ranges, the one Cloudflare names in
`CF-Connecting-IP` (a header anyone else may write is never read). The address is checked before
the signature.

Every call to an existing webhook flow is kept in memory (the last 30, cleared on restart) for the
Admin's **Recent calls**: when, from which address (through Cloudflare or not), started or read,
accepted or why not (address, time, signature, key, old key retired, input, too many), the proof
(`signature: {key name}` or `key`) and the run. Nothing the caller sent is kept: no key, signature
or body. For a signature that does not match, the detail shows the text the server checked
(nothing secret in it), so the other team can compare it with theirs. The run's context keeps the
proof in `webhook` (`by`, `key.name`, `key.fingerprint`, `address`, `viaCloudflare`).

## Configuration (appsettings.json)

```json
"DataSources": { "ServerOnlyIds": [98, 99] },
"ServerFlows": {
  "Enabled": false, "RunWorker": true, "WorkerConcurrency": 4, "SweepSeconds": 30, "LeaseSeconds": 300,
  "MissedTriggerLookbackHours": 48, "MaxRunPickups": 30, "DefaultTimeZone": "Asia/Riyadh",
  "MaxDefinitionLength": 262144, "KeepRunsDays": 0, "HoldSeconds": 30,
  "PageStartsPerMinute": 20, "WebhookCallsPerMinute": 60, "WebhookWrongKeysPerMinute": 10,
  "SignatureMaxAgeSeconds": 300, "ReadCloudflareAddress": true, "CloudflareRanges": null,
  "ScheduleCatchUpHours": 24, "AlertQuietMinutes": 5,
  "Tokens": {}
}
```

- `RunWorker`: whether this process does the work (calls and sweeps). Runs that pages, other
  systems and payments start are created in every process.
- `LeaseSeconds`: at least 300. `MaxRunPickups`: pickups that ended unexpectedly (a fault, the
  server stopping during a call); planned waits do not count.
- `HoldSeconds` (5-90): the longest one request of a page or another system is held open; a flow
  that lets its caller wait longer (`waitSec`, up to 300) has it ask again.
- `KeepRunsDays`: finished runs are cleared after this many days (a flow's `privacy.keepRunsDays`
  wins; 0 = keep). A cleared run keeps its row (run key, status, subject, dates) so nothing starts
  twice; its data, outputs and steps are deleted. A page or caller asking about it gets it as
  finished (done, or failed with the site's sentence) without results; for a once-per-journey or
  once-per-customer flow that stays the answer for that journey or customer.
- `WebhookCallsPerMinute` counts the calls that prove they are the other system's (signature or
  key), per flow; `WebhookWrongKeysPerMinute` every other call (a refused address, no or a wrong
  proof, an unknown site or flow) per site, flow and calling address, never from a flow's allowance.
- `SignatureMaxAgeSeconds` (30-900): how far a signed request's time may be from the server's.
  `ReadCloudflareAddress`: read `CF-Connecting-IP` from Cloudflare's ranges (`CloudflareRanges`
  replaces the built-in list of cloudflare.com/ips; `[]` never reads it). No other forwarded-address
  header is read by the flows; the host's forwarded-headers set-up gives the address the request
  came from.
- `DataSources:ServerOnlyIds`: data sources only server code may run (`ApiInvoker` refuses
  browsers, admins included). Put every data source that **changes** insurer data here; the Admin
  warns when a flow writes through one that is not listed.
- All values reload without a restart, except `WorkerConcurrency`.
- IIS: set the app pool to **AlwaysRunning** with preload, or an idle pool stops the worker until
  the next request (runs then resume late; never twice).

## API

Pages (signed in; `siteId` and `cultureId` headers): `POST api/ServerFlows/run` `{ flow, input,
wait }` starts (or joins) a run, `POST api/ServerFlows/result` reads the run a journey, a customer,
an input value or a payment (`input.orderId`) already has, `GET api/ServerFlows/run/{id}?wait=`.
All answer `{ runId, status: running | done | failed, results, message, waitSec }`.

Other systems: `POST api/ServerFlows/hook/{siteId}/{flow}` (headers `X-Flow-Timestamp` and
`X-Flow-Signature`, or `X-Flow-Key`), `GET .../run/{runId}?wait=` (signed the same way, with the
hash of an empty body). The body is at most 64 KB of JSON. The address must be in
`CypherSecurity:ExecludedApis` and `ApiWrapperExecludedApis`.

Admin (`api/ServerFlows/admin`, admins of the site in the `siteId` header, checked in the
database):

| | |
|---|---|
| `GET flows`, `GET flows/{id}` | versions; one version, its JSON and its issues |
| `POST flows/validate` `{definition}` | check without saving |
| `POST flows` `{name, note, definition}` | save the next version and switch it on |
| `POST flows/{id}/activate` `{checksum}`, `POST flows/{id}/archive` | switch a version on, or off |
| `POST flows/{id}/try` `{run, orderId, userId, input}` | Try it: simulate (calls nothing), or run for real (payment, order status, schedule) |
| `GET page-flows`, `GET settings`, `POST webhook-key`, `POST schedule-preview` | what the editors offer |
| `POST webhook-keypair` | a new P-256 key pair: `{ privateKeyPem, privateKeyJwk, publicKey }`, shown once (`no-store`). The Admin makes the pair in the browser (WebCrypto) and sends only the public key to `webhook-public-key`; it calls this only where the browser cannot (plain http), with `X-Skip-Logging` so a request log never keeps the answer |
| `POST webhook-public-key` `{text}` | reads a pasted public key: `{ key, type, fingerprint, shortFingerprint, pem }`, or 400 `{ error }` |
| `GET webhook-calls?flow=` | a webhook flow's recent calls, newest first |
| `GET runs?status=&search=&flowName=&from=&to=&page=`, `GET runs/counts` | runs |
| `GET runs/{id}`, `GET runs/{id}/steps/{stepId}`, `GET runs/{id}/next-body?request=` | a run, one attempt, the body a request would send |
| `POST runs/{id}/retry`, `decide`, `resend`, `send-again`, `stop` | act on a run; refused ("the run moved on") when it changed since it was opened |

## Database

Three tables: `ServerFlows_ServerFlow` (versions), `ServerFlows_ServerFlowRun`,
`ServerFlows_ServerFlowStep`. Entities in `Entitys/Entity/ServerFlows`, indexes in
`Entitys/Data/ServerFlowsCustomModelBuilder.cs`.

**Open item for the backend team:** the EF migration is not generated in this branch (the MySQL
design-time context needs the web host and a database). Generate it the usual way for each provider
(`AddServerFlows`). On a database where the tables were created by the MySQL script
(`der3-server-flows.sql`, same names), mark that migration as applied instead of running it.

## Also changed in this branch

- `ApiMapperService`: refuses `DataSources:ServerOnlyIds` outside a server scope
  (`ServerExecutionContext`, scoped, registered in Core), for the data source invoked and for every
  data source it chains to.
- Engine `OrderCancellationBackgroundService`: never cancels an order whose payment succeeded or is
  on hold.

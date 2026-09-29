# REPO_NOTES — что найдено в репозитории (Этап 0)

- Дата: 2026-09-29
- Базовый коммит: `300d2d9` (`master`), ветка работы: `feature/knowledge-service`
- Метод: прочитаны все отслеживаемые файлы репозитория (их 24, не считая `docs/`). Кода мало, поэтому ниже — полный инвентарь, а не выборка.

Главный вывод: репозиторий — **пустой каркас**. В нём есть подключения к OpenSearch и к OpenAI-совместимому LLM, но нет ни FRIDA, ни моделей вики/оргструктуры, ни маппингов индексов, ни текущего поиска, ни golden set, ни тестов. Всё, чего нет, вынесено в `OPEN_QUESTIONS.md` со значением по умолчанию.

---

## 0. Ответы на таблицу раздела 0 ТЗ

Таблица в ТЗ не заполнена, ниже — что удалось найти самостоятельно.

| Параметр | Найдено в репозитории | Что делаю | Вопрос |
|---|---|---|---|
| Путь к golden set | Нет. В репозитории нет файлов данных: только код, файлы проектов и решения, конфигурация, `readme.md` и `docs/`. | Адаптер под простой JSONL/CSV, реальный формат — после ответа | Q11 |
| Индекс вики в OpenSearch | Имя не найдено. Единственная ссылка на индекс — `Opensearch:Index` = `"index"` (плейсхолдер) в `Web/appsettings.json`, используется как `DefaultIndex` клиента. Чей это индекс — неизвестно. | Имя и поля индекса — в конфигурации KS | Q6 |
| Индекс оргструктуры | Не найден | Имя и поля — в конфигурации KS | Q7 |
| HTTP API оргсервиса | Нет клиента | `IOrgSource` читает из индекса (ТЗ 4.3 это допускает) | Q7 |
| Клиент/контракт FRIDA | Нет. FRIDA упомянута только в `readme.md` («FRIDA — embedding model») | Порт + фейк + адаптер-заглушка | Q4, Q5 |
| Подключение Gemma | `Assistant/DependencyInjection.cs`: `OpenAIClient` → `IChatClient` (Microsoft.Extensions.AI), OpenAI-совместимый эндпоинт `{Url}/v1` | `IStructuredLlm` поверх этого `IChatClient` | Q8, Q16 |
| Куда класть новый код | Явного указания нет | Новые проекты в корне рядом с существующими; HTTP API — в существующий `Web` | Q14, Q15 |

---

## 1. Сборка и среда

| Что | Значение | Где |
|---|---|---|
| SDK | `10.0.108`, `rollForward: latestMajor`, `allowPrerelease: false` → подходит любой стабильный SDK ≥ 10.0.108 | `global.json` |
| Целевой фреймворк | `net10.0` для всех проектов | `Directory.Build.props` |
| Nullable / ImplicitUsings | `enable` / `enable` | `Directory.Build.props` |
| **TreatWarningsAsErrors** | **`true`** — любое предупреждение (включая nullable) ломает сборку. Новый код должен собираться без предупреждений. | `Directory.Build.props` |
| NoWarn | `NU1803;CS1591;NU1507;NU1301` | `Directory.Build.props` |
| Версии пакетов | Central Package Management (`ManagePackageVersionsCentrally=true`); версии только в `Directory.Packages.props`, в `.csproj` — `PackageReference` без версии | `Directory.Packages.props` |
| `nuget.config` | Нет в репозитории | — |
| Формат решения | Классический `AIKnowledge.sln` (не `.slnx`), папки решения `Contracts`, `Infrastructure`, `Solution Items` | `AIKnowledge.sln` |

Наблюдение про NuGet: подавленные `NU1803` (restore из HTTP-источника), `NU1507` (несколько источников при CPM без source mapping) и `NU1301` (источник недоступен) — косвенный признак того, что в закрытом контуре используется внутреннее HTTP-зеркало NuGet. Значит, каждый новый пакет должен быть доступен в этом зеркале (Q17). Это предположение, в репозитории прямого подтверждения нет.

Пакеты сейчас (`Directory.Packages.props`): `MediatR 12.5.0`, `Microsoft.Agents.AI 1.22.0`, `Microsoft.Agents.AI.OpenAI 1.22.0`, `Microsoft.AspNetCore.OpenApi 10.0.12`, `Microsoft.Extensions.Hosting.Abstractions 10.0.12`, `Microsoft.Extensions.Options.ConfigurationExtensions 10.0.12`, `ModelContextProtocol 2.2.0`, `OpenSearch.Client 2.1.0`, `OpenSearch.Net 2.1.0`.

### Состояние SDK в этой облачной сессии

- `dotnet` в контейнере не установлен.
- `dotnet-install.sh --jsonfile global.json` (скрипт получен с `raw.githubusercontent.com/dotnet/install-scripts`) падает с HTTP 403: политика сети сессии не пускает на `builds.dotnet.microsoft.com` и `ci.dot.net`.
- В apt (Ubuntu 24.04, `noble-updates`) доступен `dotnet-sdk-10.0` версии `10.0.112` — он удовлетворяет `global.json`, но установку этим способом пользователь отклонил.
- `api.nuget.org` из сессии доступен (HTTP 200), то есть после установки SDK restore должен пройти.
- **Итог: `dotnet build` в этой сессии не проверен.** Для Этапа 1 это блокер (Q1).

---

## 2. Структура решения

| Проект | Тип | Пакеты | Ссылки на проекты | Содержимое |
|---|---|---|---|---|
| `Web` | `Microsoft.NET.Sdk.Web` | MediatR, Microsoft.AspNetCore.OpenApi, ModelContextProtocol | Application, Assistant, Opensearch | `Program.cs`, `DependencyInjection.cs` (`AddWeb`), `appsettings*.json` |
| `Application` | classlib | MediatR, Hosting.Abstractions | — | `AddApplication()`: регистрация MediatR из сборки. Обработчиков нет. |
| `Assistant` | classlib (папка решения `Infrastructure`) | Microsoft.Agents.AI, Microsoft.Agents.AI.OpenAI, Hosting.Abstractions | — | `AddAssistant()`: `OpenAIClient` + `IChatClient`; `Common/AssistantOptions.cs` |
| `Opensearch` | classlib (папка `Infrastructure`) | OpenSearch.Client, OpenSearch.Net, Hosting.Abstractions, Options.ConfigurationExtensions | — | `AddOpensearch()`: `IOpenSearchClient`; `Options/OpensearchOptions.cs` |
| `Application.Contracts` | classlib (папка `Contracts`) | — | — | Пустой, только `.csproj` |
| `Assistant.Contracts` | classlib (папка `Contracts`) | — | — | Пустой |
| `Opensearch.Contracts` | classlib (папка `Contracts`) | — | — | Пустой |

Проекты `*.Contracts` ни на что не ссылаются, и на них никто не ссылается. Судя по раскладке, задуманный шаблон такой: модуль `X` + `X.Contracts` для его публичных DTO.

Тестовых проектов нет.

---

## 3. Стиль и соглашения

**DI.** В каждом проекте есть `public static class DependencyInjection` с методом-расширением `AddXxx(this IHostApplicationBuilder builder)`, который возвращает `builder` для цепочки. Внутри — либо `builder.Services...`, либо приватный `AddXxxServices(this IServiceCollection)`. В `Program.cs` модули подключаются цепочкой:

```csharp
builder
    .AddApplication()
    .AddAssistant()
    .AddOpensearch()
    .AddWeb();
```

Следствие для KS: общий метод `AddKnowledgeService(this IHostApplicationBuilder)` естественно ложится и на `WebApplicationBuilder` (Web), и на `HostApplicationBuilder` (CLI) — ТЗ требует одну регистрацию для Api и Cli.

**Options.** `public sealed class XxxOptions` со свойствами `public required T Prop { get; init; }`, в папке `Options/` (Opensearch) или `Common/` (Assistant). Привязка — `builder.Services.AddOptions<T>().BindConfiguration("Section")`, без `ValidateOnStart` и DataAnnotations. Получение — `IOptions<T>.Value` внутри фабрики singleton.

**Имена и форматирование.**
- Пространство имён = имя проекта (`Opensearch`, не `OpenSearch`; `Opensearch.Options`, `Assistant.Common`); file-scoped namespaces.
- `var`, `static`-лямбды в фабриках DI, `sealed`-классы для options, `internal` для класса DI в хосте (`Web.DependencyInjection`).
- Отступ 4 пробела в `.cs`/`.csproj`, 2 пробела в `Directory.Packages.props`; переводы строк LF.
- UTF-8 BOM непоследовательно: с BOM — `.sln`, все `.csproj`, кроме `Web.csproj`, `Assistant/DependencyInjection.cs`, `launchSettings.json`; остальные — без BOM. `.editorconfig` нет.
- Комментариев и XML-doc в коде нет (`CS1591` подавлено). Документация (`readme.md`, ТЗ) — на русском.

**Логирование.** Стандартный `ILogger` ASP.NET Core, уровни в секции `Logging` в `appsettings.json`. Serilog/OpenTelemetry нет.

**Конфигурация и секреты.** Секреты в `appsettings.json` — строками-плейсхолдерами (`"Url": "Url"`, `"Username": "Username"`, `"Password": "Password"`). В `.gitignore` есть `.env`; user-secrets (`UserSecretsId`) не настроены. Вывод: реальные значения подаются снаружи — вероятно, переменными окружения (Q2).

**Имена секций.** Код привязывает `"Opensearch"`, а в `appsettings.json` секция называется `"OpenSearch"`. Ключи конфигурации .NET регистронезависимы, так что это работает.

**Web-хост (`Web/Program.cs`, `Web/DependencyInjection.cs`).**
- Контроллеры: `AddControllers()` + `MapControllers()`, но самих контроллеров нет. Minimal API не используется.
- OpenAPI (`AddOpenApi`/`MapOpenApi`) — только в Development.
- `AddHealthChecks()` + `MapHealthChecks("/health")` без зарегистрированных проверок. ТЗ требует `/health` с проверками как у `check`: их можно добавить через `IHealthCheck` в этот же конвейер.
- `AddResponseCompression(...)` зарегистрирован, но `app.UseResponseCompression()` не вызывается, поэтому сжатие фактически не работает. К KS это не относится, просто фиксирую.
- Аутентификации нет (`UseAuthorization()` без `AddAuthentication`).
- `UseHttpsRedirection()`, `UseStaticFiles()` (`wwwroot/` объявлен в `.csproj`, но в git его нет).

**MediatR.** Зарегистрирован в `Application`, обработчиков нет. Соглашение «контроллер → MediatR → обработчик» подразумевается, но не подтверждено ни одним примером (Q15).

**MCP.** Пакет `ModelContextProtocol 2.2.0` подключён к `Web`, но не используется. Для хостинга MCP в ASP.NET Core (Этап 8) обычно нужен ещё `ModelContextProtocol.AspNetCore` — проверить на Этапе 8.

**Тесты.** Тестовых проектов и тестового фреймворка нет (Q17).

**`.gitignore`.** Стандартный шаблон Visual Studio. `out/` **не** игнорируется (проверено через `git check-ignore`), а ТЗ пишет туда результаты dry-run и eval. На Этапе 1 добавить `out/` в `.gitignore`.

---

## 4. Подключение Gemma (LLM)

Файлы: `Assistant/DependencyInjection.cs`, `Assistant/Common/AssistantOptions.cs`.

```csharp
services.AddSingleton<OpenAIClient>(sp => new OpenAIClient(
    new ApiKeyCredential("RandomKey"),
    new OpenAIClientOptions { Endpoint = new Uri(assistantOptions.Url + "/v1") }));

services.AddSingleton<IChatClient>(sp =>
    openAiClient.GetChatClient(assistantOptions.Model).AsIChatClient());
```

- **Клиент:** `Microsoft.Extensions.AI.IChatClient` поверх `OpenAI.OpenAIClient` (пакеты приходят транзитивно через `Microsoft.Agents.AI.OpenAI 1.22.0`). Сервер — OpenAI-совместимый, базовый путь `{Url}/v1`. Какой именно сервер (vLLM, Ollama, llama.cpp, TGI…), неизвестно.
- **Модель:** имя берётся из `AssistantOptions.Model`. То, что это Gemma 4 31B, известно только из ТЗ.
- **Structured output:** нигде не используется. `IChatClient` позволяет передать `ChatOptions.ResponseFormat = ChatResponseFormat.ForJsonSchema(...)`, что превращается в OpenAI `response_format: { type: "json_schema" }`. Поддерживает ли это сервер — неизвестно (Q8).
- **Temperature / режим размышлений:** нигде не настраиваются. Как сервер отключает thinking, неизвестно (Q8).
- **Дефект 1 — options не привязаны к конфигурации.** Для `AssistantOptions` нет `AddOptions<AssistantOptions>().BindConfiguration(...)`, и в `appsettings.json` нет соответствующей секции. `IOptions<AssistantOptions>.Value` вернёт объект с `null` в `required`-свойствах (`required` проверяется только компилятором). Дальше:
    - эндпоинт станет `new Uri(null + "/v1")` = `new Uri("/v1")`: в Windows это `UriFormatException`, в Linux — неявный файловый URI `file:///v1`;
    - `GetChatClient(null)` получит пустое имя модели.

    Сейчас `OpenAIClient`/`IChatClient` никто не запрашивает, поэтому приложение стартует. Но как только KS начнёт использовать `IChatClient`, вызовы LLM работать не будут (Q16).
- **Дефект 2 — `AssistantOptions.ApiKey` игнорируется.** Ключ захардкожен как `"RandomKey"` — видимо, сервер ключ не проверяет (Q16).
- **Agent Framework:** `Microsoft.Agents.AI` подключён, но агентов нет; используется только расширение `AsIChatClient()`.

Вывод для KS: `IStructuredLlm` — адаптер над `IChatClient`, который регистрирует `Assistant` (ТЗ: «через существующее подключение»). Это требует минимальной правки привязки options в `Assistant` (Q16).

---

## 5. FRIDA (эмбеддинги)

- **В репозитории нет ничего:** ни клиента, ни контракта, ни URL, ни формата запроса. Есть только строка в `readme.md`.
- Поэтому по ТЗ 6.0 **нельзя установить из кода**, добавляет ли сервис префиксы `search_query: ` / `search_document: ` сам, какова размерность, какие лимиты батча, отдаёт ли сервис число токенов или признак обрезки (Q4, Q5).
- Справочно, из публичной карточки модели `ai-forever/FRIDA` (не из репозитория, **не контракт сервиса**): размерность эмбеддинга 1536, максимум 512 токенов на вход, префиксы задач `search_query: `, `search_document: ` (и другие: `paraphrase: `, `categorize: ` …). Это совпадает с ТЗ, но как именно сервис в контуре оборачивает модель, неизвестно.
- Решение по ТЗ 3.1 и 1.3: `IEmbeddingClient` + детерминированный фейк для тестов + адаптер-заглушка с понятной ошибкой в `check`, пока не будет контракта. Размерность — из конфигурации (`KnowledgeService:Embeddings:Dimensions`, по умолчанию 1536); `check` сверяет её с фактическим ответом.

---

## 6. OpenSearch

Файлы: `Opensearch/DependencyInjection.cs`, `Opensearch/Options/OpensearchOptions.cs`.

- **Клиент:** `OpenSearch.Client 2.1.0` (высокоуровневый) + `OpenSearch.Net 2.1.0` (низкоуровневый). Singleton `IOpenSearchClient` через `ConnectionSettings(new Uri(Url)).BasicAuthentication(Username, Password).DefaultIndex(Index)`.
- **Options:** `Url`, `Index`, `Username`, `Password` из секции `Opensearch`. Никаких настроек TLS или сертификатов.
- **Версия сервера:** в репозитории не видна (Q10). От неё зависит поддержка efficient filtering в kNN; по документации OpenSearch — для движка `lucene` с 2.4, для `faiss` с 2.9. Проверить на реальном кластере: `check` должен печатать версию.
- **Маппинги индексов вики и оргструктуры:** в репозитории отсутствуют (Q6, Q7).
- **Код запросов:** нет ни одного запроса, поиска или маппинга. Клиент только регистрируется.
- Для KS удобно переиспользовать этот же `IOpenSearchClient`: подключение и учётные данные уже настроены. KS всегда указывает индекс явно, поэтому `DefaultIndex` не мешает. Тела запросов и маппингов для `ks-*` удобно строить как JSON (`System.Text.Json.Nodes`) и отправлять через `client.LowLevel`. Это прямо даёт тестируемость по JSON (ТЗ 3.3) и снимает зависимость от того, какие kNN-конструкции поддерживает высокоуровневый клиент 2.1.0.

---

## 7. Источники данных

**Вики.** Нет модели статьи, клиента API и маппинга индекса. Неизвестно:
- где полный HTML (поле индекса? только текст?);
- где дата изменения;
- есть ли признак удаления и права доступа.

По ТЗ 4.3 самый быстрый путь — чтение из существующего индекса (только чтение, PIT + `search_after`). Имена полей (id, title, url, html, updated_at, access) выношу в конфигурацию. Вопрос о фактическом маппинге — Q6.

**Оргструктура.** Нет клиента оргсервиса, модели и маппинга индекса. Неизвестны:
- типы записей (организация / подразделение / сотрудник) и их идентификаторы;
- полное и краткое наименование, аббревиатура;
- ссылка на родителя, руководитель, ФИО по частям или строкой.

`IOrgSource.ListAllAsync` и `GetUnitAsync` будут читать из индекса оргструктуры (ТЗ 4.3 это допускает, если API нет); поля — в конфигурации. Вопрос — Q7.

---

## 8. Текущий поиск чата

**Отсутствует.** В репозитории нет ни чата, ни агентов, ни инструментов, ни запросов к индексу вики. `Assistant` только регистрирует `IChatClient`, `Opensearch` — только `IOpenSearchClient`. Построить `LegacySearchAdapter` из кода репозитория (ТЗ 9.3) невозможно (Q12).

---

## 9. Golden set

**Не найден.** Путь, формат, объём и категории неизвестны (Q11).

---

## 10. Права доступа (раздел 11 ТЗ)

Информации нет: ни модели прав у статей, ни фильтрации в текущем чате (которого в репозитории нет). По ТЗ: `Access.Enabled = false`, поле `access` в маппинге создаётся (Q13).

---

## 11. Реранкер

В репозитории нет. По ТЗ адаптер по умолчанию — HTTP в формате Hugging Face TEI `/rerank`, `Reranker.Enabled = false` (Q9).

---

## 12. Прочее

- `CLAUDE.md` содержит только импорт `@docs/knowledge-service/SPEC.md`. В исходном снимке (`0812bf8`) файл назывался `CPEC.md` (латинские буквы), и импорт не срабатывал. В `300d2d9` файл переименован в `SPEC.md` — теперь импорт корректен.
- Предупреждения компилятора — ошибки, поэтому новые пакеты с предупреждениями анализаторов или устаревшими API могут ломать сборку; проверять сразу при добавлении.

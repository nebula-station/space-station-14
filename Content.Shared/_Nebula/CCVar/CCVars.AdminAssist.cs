// SPDX-FileCopyrightText: 2026 yellow-apple34
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._Nebula.CCVar;

public sealed partial class NCCVars
{
    public static readonly CVarDef<bool> AHelpPersist =
        CVarDef.Create("admin_assist.ahelp_persist", false, CVar.SERVERONLY);

    /// <summary>
    /// Пустое значение отключает эндпоинт. Не должен совпадать с admin.api_token.
    /// </summary>
    public static readonly CVarDef<string> AdminAssistRoundStateToken =
        CVarDef.Create("admin_assist.round_state_token", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// Запросов с одного адреса за 10 секунд.
    /// </summary>
    public static readonly CVarDef<int> AdminAssistRoundStateRateLimit =
        CVarDef.Create("admin_assist.round_state_rate_limit", 30, CVar.SERVERONLY);

    /// <summary>
    /// Только Postgres; роли игры нужен SELECT из ops/03 сайдкара.
    /// </summary>
    public static readonly CVarDef<bool> AdminAssistInGameBriefings =
        CVarDef.Create("admin_assist.ingame_briefings", false, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Новые значения admin_assist.* доходят до сайдкара только после его перезапуска.
    /// </summary>
    public static readonly CVarDef<bool> AdminAssistSidecar =
        CVarDef.Create("admin_assist.sidecar", false, CVar.SERVERONLY);

    /// <summary>
    /// Python 3.12–3.14.
    /// </summary>
    public static readonly CVarDef<string> AdminAssistSidecarPython =
        CVarDef.Create("admin_assist.sidecar_python", "python3", CVar.SERVERONLY);

    /// <summary>
    /// Пустое значение: admin_assist рядом с сервером.
    /// </summary>
    public static readonly CVarDef<string> AdminAssistSidecarDir =
        CVarDef.Create("admin_assist.sidecar_dir", string.Empty, CVar.SERVERONLY);

    public static readonly CVarDef<string> AdminAssistGameDsn =
        CVarDef.Create("admin_assist.game_dsn", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> AdminAssistOwnDsn =
        CVarDef.Create("admin_assist.own_dsn", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<int> AdminAssistDbStatementTimeoutMs =
        CVarDef.Create("admin_assist.db_statement_timeout_ms", 5000, CVar.SERVERONLY);

    public static readonly CVarDef<string> AdminAssistKidMeshBaseUrl =
        CVarDef.Create("admin_assist.kidmesh_base_url", "https://api.kidmesh.best/v1", CVar.SERVERONLY);

    public static readonly CVarDef<float> AdminAssistKidMeshTimeoutS =
        CVarDef.Create("admin_assist.kidmesh_timeout_s", 30f, CVar.SERVERONLY);

    public static readonly CVarDef<int> AdminAssistKidMeshMaxRetries =
        CVarDef.Create("admin_assist.kidmesh_max_retries", 2, CVar.SERVERONLY);

    /// <summary>
    /// Пауза тира после HTTP 401/402/403.
    /// </summary>
    public static readonly CVarDef<float> AdminAssistKidMeshPauseS =
        CVarDef.Create("admin_assist.kidmesh_pause_s", 600f, CVar.SERVERONLY);

    /// <summary>
    /// KIDMesh подменяет system-сообщение на некоторых моделях. Оставьте включённым.
    /// </summary>
    public static readonly CVarDef<bool> AdminAssistKidMeshFoldSystem =
        CVarDef.Create("admin_assist.kidmesh_fold_system", true, CVar.SERVERONLY);

    /// <summary>
    /// Пустое значение отключает проверку.
    /// </summary>
    public static readonly CVarDef<string> AdminAssistKidMeshCatalogUrl =
        CVarDef.Create("admin_assist.kidmesh_catalog_url", "https://kidmesh.best/api/models", CVar.SERVERONLY);

    public static readonly CVarDef<float> AdminAssistCatalogPollS =
        CVarDef.Create("admin_assist.catalog_poll_s", 600f, CVar.SERVERONLY);

    public static readonly CVarDef<string> AdminAssistKidMeshKey =
        CVarDef.Create("admin_assist.kidmesh_key", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> AdminAssistModelMainId =
        CVarDef.Create("admin_assist.model_main_id", "deepseek-v4.1-flash-1017", CVar.SERVERONLY);

    /// <summary>
    /// Пустой берётся из admin_assist.kidmesh_key.
    /// </summary>
    public static readonly CVarDef<string> AdminAssistModelMainKey =
        CVarDef.Create("admin_assist.model_main_key", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<float> AdminAssistModelMainMultiplier =
        CVarDef.Create("admin_assist.model_main_multiplier", 1.0f, CVar.SERVERONLY);

    public static readonly CVarDef<string> AdminAssistModelFallbackId =
        CVarDef.Create("admin_assist.model_fallback_id", "glm-5.3", CVar.SERVERONLY);

    public static readonly CVarDef<string> AdminAssistModelFallbackKey =
        CVarDef.Create("admin_assist.model_fallback_key", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<float> AdminAssistModelFallbackMultiplier =
        CVarDef.Create("admin_assist.model_fallback_multiplier", 2.5f, CVar.SERVERONLY);

    public static readonly CVarDef<string> AdminAssistModelSmallId =
        CVarDef.Create("admin_assist.model_small_id", "gpt-6-luna", CVar.SERVERONLY);

    public static readonly CVarDef<string> AdminAssistModelSmallKey =
        CVarDef.Create("admin_assist.model_small_key", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<float> AdminAssistModelSmallMultiplier =
        CVarDef.Create("admin_assist.model_small_multiplier", 0.9f, CVar.SERVERONLY);

    /// <summary>
    /// "&lt;тир&gt;:&lt;уровень рассуждений&gt;", тир: main|fallback|small, уровень: none|low|medium|high.
    /// </summary>
    public static readonly CVarDef<string> AdminAssistFeatureAHelpSummary =
        CVarDef.Create("admin_assist.feature_ahelp_summary", "main:low", CVar.SERVERONLY);

    public static readonly CVarDef<string> AdminAssistFeatureDoneTyping =
        CVarDef.Create("admin_assist.feature_done_typing", "main:none", CVar.SERVERONLY);

    public static readonly CVarDef<float> AdminAssistPriceRubPerMTok =
        CVarDef.Create("admin_assist.price_rub_per_mtok", 4.35f, CVar.SERVERONLY);

    public static readonly CVarDef<float> AdminAssistCachedInputFactor =
        CVarDef.Create("admin_assist.cached_input_factor", 0.5f, CVar.SERVERONLY);

    public static readonly CVarDef<float> AdminAssistBudgetRub =
        CVarDef.Create("admin_assist.budget_rub", 500f, CVar.SERVERONLY);

    public static readonly CVarDef<float> AdminAssistBudgetAlertFraction =
        CVarDef.Create("admin_assist.budget_alert_fraction", 0.8f, CVar.SERVERONLY);

    /// <summary>
    /// Пустое значение у сайдкара, запущенного сервером, подставляется по status.bind.
    /// </summary>
    public static readonly CVarDef<string> AdminAssistRoundStateUrl =
        CVarDef.Create("admin_assist.round_state_url", string.Empty, CVar.SERVERONLY);

    public static readonly CVarDef<float> AdminAssistRoundStateTimeoutS =
        CVarDef.Create("admin_assist.round_state_timeout_s", 3f, CVar.SERVERONLY);

    public static readonly CVarDef<int> AdminAssistContextBeforeS =
        CVarDef.Create("admin_assist.context_before_s", 300, CVar.SERVERONLY);

    public static readonly CVarDef<int> AdminAssistContextAfterS =
        CVarDef.Create("admin_assist.context_after_s", 10, CVar.SERVERONLY);

    public static readonly CVarDef<int> AdminAssistContextMaxLogLines =
        CVarDef.Create("admin_assist.context_max_log_lines", 150, CVar.SERVERONLY);

    public static readonly CVarDef<int> AdminAssistContextMaxParticipants =
        CVarDef.Create("admin_assist.context_max_participants", 6, CVar.SERVERONLY);

    public static readonly CVarDef<float> AdminAssistNewPlayerHours =
        CVarDef.Create("admin_assist.new_player_hours", 10f, CVar.SERVERONLY);

    /// <summary>
    /// Одно правило на строку с номером; путь от папки сайдкара.
    /// </summary>
    public static readonly CVarDef<string> AdminAssistRulesFile =
        CVarDef.Create("admin_assist.rules_file", "rules/nebula.ru.txt", CVar.SERVERONLY);

    public static readonly CVarDef<float> AdminAssistPollS =
        CVarDef.Create("admin_assist.poll_s", 2f, CVar.SERVERONLY);

    public static readonly CVarDef<float> AdminAssistDetectorDebounceS =
        CVarDef.Create("admin_assist.detector_debounce_s", 6f, CVar.SERVERONLY);

    public static readonly CVarDef<float> AdminAssistDetectorForceS =
        CVarDef.Create("admin_assist.detector_force_s", 75f, CVar.SERVERONLY);

    public static readonly CVarDef<float> AdminAssistTicketGapS =
        CVarDef.Create("admin_assist.ticket_gap_s", 900f, CVar.SERVERONLY);

    public static readonly CVarDef<float> AdminAssistTicketStaleS =
        CVarDef.Create("admin_assist.ticket_stale_s", 600f, CVar.SERVERONLY);

    public static readonly CVarDef<int> AdminAssistTicketMaxSummaries =
        CVarDef.Create("admin_assist.ticket_max_summaries", 3, CVar.SERVERONLY);

    public static readonly CVarDef<int> AdminAssistSummaryMaxTokens =
        CVarDef.Create("admin_assist.summary_max_tokens", 4000, CVar.SERVERONLY);

    public static readonly CVarDef<int> AdminAssistMaxParallelCalls =
        CVarDef.Create("admin_assist.max_parallel_calls", 3, CVar.SERVERONLY);

    public static readonly CVarDef<float> AdminAssistFeedbackPollS =
        CVarDef.Create("admin_assist.feedback_poll_s", 300f, CVar.SERVERONLY);

    public static readonly CVarDef<bool> AdminAssistErpTextToModel =
        CVarDef.Create("admin_assist.erp_text_to_model", false, CVar.SERVERONLY);
}

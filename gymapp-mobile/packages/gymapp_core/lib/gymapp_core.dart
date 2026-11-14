/// GymApp Core - Shared package for GymApp Flutter apps.
///
/// Provides models, network layer, theme, widgets, localization,
/// services, utilities, and error handling used across all 3 apps.
library gymapp_core;

// Constants
export 'src/constants/api_endpoints.dart';
export 'src/constants/app_constants.dart';
export 'src/constants/storage_keys.dart';
export 'src/constants/feature_codes.dart';

// Types (zero-cost value types / ids)
export 'src/types/app_language.dart';
export 'src/types/auth_tokens.dart';
export 'src/types/entity_ids.dart';
export 'src/types/measurements.dart';

// Errors
export 'src/errors/failure.dart';
export 'src/errors/either_extensions.dart';

// Models
export 'src/models/user_model.dart';
export 'src/models/gym_model.dart';
export 'src/models/exercise_model.dart';
export 'src/models/workout_model.dart';
export 'src/models/body_metric_model.dart';
export 'src/models/session_model.dart';
export 'src/models/message_model.dart';
export 'src/models/notification_model.dart';
export 'src/models/meal_plan_model.dart';
export 'src/models/payment_model.dart';
export 'src/models/plan_features_model.dart';
export 'src/models/pagination_model.dart';
export 'src/models/api_response_model.dart';
export 'src/models/auth_model.dart';
export 'src/models/announcement_model.dart';
export 'src/models/leaderboard_model.dart';
export 'src/models/trainer_rating_model.dart';
export 'src/models/pending_testimonial_model.dart';
export 'src/models/check_in_model.dart';
export 'src/models/class_model.dart';
export 'src/models/staff_model.dart';
export 'src/models/gym_branding_model.dart';
export 'src/models/food_item_model.dart';
export 'src/models/water_log_model.dart';
export 'src/models/progress_photo_model.dart';
export 'src/models/streak_model.dart';
export 'src/models/session_package_model.dart';
export 'src/models/trainer_availability_model.dart';
export 'src/models/weekly_availability_model.dart';
export 'src/models/remote_workout_model.dart';
export 'src/models/pt_conversion_models.dart';

// Feature flags (per-gym presets + overrides)
export 'src/feature_flags/feature_flag_keys.dart';
export 'src/feature_flags/feature_flags.dart';
export 'src/feature_flags/feature_flag_service.dart';
export 'src/feature_flags/feature_flags_provider.dart';
export 'src/feature_flags/feature_flag_gate.dart';
export 'src/feature_flags/feature_route_guard.dart';
export 'src/feature_flags/feature_flag_signals.dart';

// Progress journey (pt.progress_measurements)
export 'src/progress_journey/progress_journey_model.dart';
export 'src/progress_journey/progress_metric.dart';
export 'src/progress_journey/progress_journey.dart';
export 'src/progress_journey/progress_journey_strings.dart';
export 'src/progress_journey/widgets/progress_journey_view.dart';
export 'src/progress_journey/widgets/since_day_one_card.dart';
export 'src/progress_journey/widgets/goal_picker_sheet.dart';
export 'src/progress_journey/widgets/progress_wins_card.dart';
export 'src/progress_journey/widgets/next_check_in_card.dart';
export 'src/progress_journey/widgets/change_chip.dart' show ChangeChip, toneColor;
export 'src/progress_journey/measurement_form/measurement_rules.dart';
export 'src/progress_journey/measurement_form/measurement_form_controller.dart';
export 'src/progress_journey/measurement_form/measurement_form_strings.dart';
export 'src/progress_journey/measurement_form/widgets/measurement_entry_page.dart';
export 'src/exercise_progress/exercise_progress_model.dart';
export 'src/exercise_progress/exercise_days.dart';
export 'src/exercise_progress/exercise_insights.dart';
export 'src/exercise_progress/exercise_progress_strings.dart';
export 'src/exercise_progress/widgets/exercise_progress_view.dart';

// Network
export 'src/network/api_base_url.dart';
export 'src/network/api_client.dart';
export 'src/network/interceptors/auth_interceptor.dart';
export 'src/network/interceptors/gym_id_interceptor.dart';
export 'src/network/interceptors/locale_interceptor.dart';
export 'src/network/interceptors/logging_interceptor.dart';
export 'src/network/interceptors/error_interceptor.dart';
export 'src/network/api_exceptions.dart';

// Services
export 'src/services/fcm_service.dart';
export 'src/services/consent_service.dart';
export 'src/services/offline_queue_service.dart';
export 'src/services/secure_storage_key.dart';
export 'src/services/secure_storage_service.dart';
export 'src/services/connectivity_service.dart';
export 'src/services/chat_service.dart';
export 'src/services/chat_realtime.dart';
export 'src/services/branding_hub_service.dart';
export 'src/services/telemetry_service.dart';
export 'src/services/telemetry_bootstrap.dart';

// Theme
export 'src/theme/app_theme.dart';
export 'src/theme/app_colors.dart';
export 'src/theme/app_typography.dart';
export 'src/theme/app_spacing.dart';
export 'src/theme/gym_branding.dart';
export 'src/theme/responsive.dart';

// Usecases
export 'src/usecases/usecase.dart';

// Utils
export 'src/utils/date_formatter.dart';
export 'src/utils/number_formatter.dart';
export 'src/utils/validators.dart';
export 'src/utils/extensions.dart';
export 'src/utils/debouncer.dart';
export 'src/utils/exercise_labels.dart';
export 'src/utils/telemetry_navigator_observer.dart';

// Widgets
export 'src/widgets/app_button.dart';
export 'src/widgets/app_text_field.dart';
export 'src/widgets/app_card.dart';
export 'src/widgets/loading_shimmer.dart';
export 'src/widgets/error_widget.dart';
export 'src/widgets/empty_state.dart';
export 'src/widgets/stat_card.dart';
export 'src/widgets/avatar_widget.dart';
export 'src/widgets/chart_widgets.dart';
export 'src/widgets/feature_gate.dart';
export 'src/widgets/gym_suspension_overlay.dart';
export 'src/widgets/upgrade_prompt.dart';
export 'src/widgets/consent_dialog.dart';
export 'src/widgets/chat_screen.dart';
export 'src/widgets/storage_limit_dialog.dart';
export 'src/widgets/rtl_scaffold.dart';
export 'src/widgets/gym_switcher.dart';
export 'src/widgets/pdpl_settings_section.dart';

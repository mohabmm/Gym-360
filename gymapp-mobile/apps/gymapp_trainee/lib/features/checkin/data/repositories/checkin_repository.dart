import 'package:gymapp_core/gymapp_core.dart';

import '../datasources/checkin_remote_datasource.dart';

class CheckInRepository {
  final CheckInRemoteDataSource _remoteDataSource;

  CheckInRepository({required CheckInRemoteDataSource remoteDataSource})
      : _remoteDataSource = remoteDataSource;

  Future<List<CheckInModel>> getCheckInHistory(String gymId) =>
      _remoteDataSource.getCheckInHistory(gymId);
}

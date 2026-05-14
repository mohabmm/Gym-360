import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/features/membership/data/datasources/membership_remote_datasource.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// Membership screen — my packages + payments. Paginated list for payments,
/// filterable list for packages.
void main() {
  late TestApiClient t;
  late MembershipRemoteDataSource ds;

  setUp(() {
    t = buildTestApiClient();
    ds = MembershipRemoteDataSource(apiClient: t.apiClient);
  });

  test('GET /billing/me/packages defaults to no status filter', () async {
    t.adapter.onGet(
      ApiEndpoints.myPackages,
      respond: (_) => t.adapter.ok([Samples.sessionPackage()]),
    );
    final pkgs = await ds.getActivePackages();
    expect(pkgs, hasLength(1));
    expect(pkgs.first.remainingSessions, 7);
    expect(t.onlyRequest.query.containsKey('status'), isFalse);
  });

  test('GET /billing/me/packages forwards status=All when provided', () async {
    t.adapter.onGet(
      ApiEndpoints.myPackages,
      respond: (_) => t.adapter.ok([]),
    );
    await ds.getActivePackages(status: 'All');
    expect(t.onlyRequest.q('status'), 'All');
  });

  test('GET /billing/me/payments returns paginated result with meta',
      () async {
    t.adapter.onGet(
      ApiEndpoints.myPayments,
      respond: (_) => t.adapter.ok(
        [Samples.payment(), Samples.payment(id: 'pay-2')],
        meta: {
          'totalCount': 2,
          'pageSize': 20,
          'currentPage': 1,
          'totalPages': 1,
        },
      ),
    );

    final page =
        await ds.getPayments(page: 1, pageSize: 20);
    expect(page.items, hasLength(2));
    expect(page.meta.totalCount, 2);
    expect(t.onlyRequest.q('page'), '1');
    expect(t.onlyRequest.q('pageSize'), '20');
  });

  test('Voided payment is parsed with Voided status (Founder Q9)', () async {
    t.adapter.onGet(
      ApiEndpoints.myPayments,
      respond: (_) => t.adapter.ok(
        [
          {
            ...Samples.payment(),
            'status': 'Voided',
            'voidedAt': '2026-04-10T12:00:00.000Z',
            'voidReason': 'Duplicate entry',
          },
        ],
        meta: {
          'totalCount': 1,
          'pageSize': 20,
          'currentPage': 1,
          'totalPages': 1,
        },
      ),
    );
    final page = await ds.getPayments();
    final p = page.items.single;
    expect(p.status, 'Voided');
    expect(p.voidReason, 'Duplicate entry');
  });
}

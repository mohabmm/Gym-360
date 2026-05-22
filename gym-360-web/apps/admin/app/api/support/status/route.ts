import { NextRequest, NextResponse } from 'next/server';

export async function PATCH(req: NextRequest) {
  const body = await req.json().catch(() => ({}));
  return NextResponse.json(
    { data: { ...body, updatedAt: new Date().toISOString() }, errors: null },
    { status: 200 },
  );
}

'use client';

import { useState } from 'react';
import { toast } from 'sonner';
import { PageHeader, StatusBadge, KpiCard, Modal, formatCurrency, formatDate } from '@gym360/ui';
import { CreditCard, DollarSign, Plus, Receipt, Package, Edit, Trash2, Users } from 'lucide-react';
import {
  useMembershipPlans, useSessionPackageTemplates, useActivePackages, usePayments,
  useCreatePayment, useVoidPayment, useCreateMembershipPlan, useUpdateMembershipPlan,
  useCreatePackageTemplate, useUpdatePackageTemplate, useAssignPackage,
} from '@/lib/hooks/useBilling';
import { useMembers } from '@/lib/hooks/useMembers';

type MembershipPlan = {
  id: string;
  name: string;
  description: string;
  durationDays: number;
  price: number;
  isActive: boolean;
  memberCount?: number;
};

type SessionPackageTemplate = {
  id: string;
  name: string;
  sessionCount: number;
  pricePerSession: number;
  totalPrice: number;
  validityDays: number;
  isActive: boolean;
};

type ActivePackage = {
  id: string;
  memberName: string;
  templateName: string;
  remaining: number;
  total: number;
  expiresAt: string;
  assignedBy: string;
};

const tabs = ['Payments', 'Plans', 'Packages'] as const;

export default function BillingPage() {
  const [activeTab, setActiveTab] = useState<(typeof tabs)[number]>('Payments');
  const [showRecordPayment, setShowRecordPayment] = useState(false);
  const [showPlanModal, setShowPlanModal] = useState(false);
  const [editingPlan, setEditingPlan] = useState<MembershipPlan | null>(null);
  const [showTemplateModal, setShowTemplateModal] = useState(false);
  const [editingTemplate, setEditingTemplate] = useState<SessionPackageTemplate | null>(null);
  const [showAssignModal, setShowAssignModal] = useState(false);
  const [assignForm, setAssignForm] = useState({ memberId: '', templateId: '' });
  const [paymentForm, setPaymentForm] = useState({ memberId: '', amount: '', type: 'membership', method: 'cash' });

  const { data: plans = [] } = useMembershipPlans();
  const { data: templates = [] } = useSessionPackageTemplates();
  const { data: activePackages = [] } = useActivePackages();
  const { data: recentPayments = [] } = usePayments(50);
  const { data: members = [] } = useMembers();
  const createPayment = useCreatePayment();
  const voidPayment = useVoidPayment();
  const createPlan = useCreateMembershipPlan();
  const updatePlan = useUpdateMembershipPlan();
  const createTemplate = useCreatePackageTemplate();
  const updateTemplate = useUpdatePackageTemplate();
  const assignPackage = useAssignPackage();

  function showApiError(err: unknown, fallback: string) {
    const msg = (err as { response?: { data?: { errors?: { message?: string }[] } } })
      ?.response?.data?.errors?.[0]?.message ?? fallback;
    toast.error(msg);
  }

  function submitAssign() {
    if (!assignForm.memberId || !assignForm.templateId) {
      toast.error('Select a member and a template.');
      return;
    }
    const member = members.find((m) => m.id === assignForm.memberId);
    const template = templates.find((t) => t.id === assignForm.templateId);
    assignPackage.mutate(assignForm, {
      onSuccess: () => {
        toast.success(`${template?.name} assigned to ${member?.name}.`);
        setAssignForm({ memberId: '', templateId: '' });
        setShowAssignModal(false);
      },
      onError: (err) => showApiError(err, 'Failed to assign package.'),
    });
  }

  function submitPayment() {
    const amount = parseFloat(paymentForm.amount);
    if (!paymentForm.memberId) {
      toast.error('Select a member.');
      return;
    }
    if (!amount || amount <= 0) {
      toast.error('Enter a valid amount.');
      return;
    }
    const member = members.find((m) => m.id === paymentForm.memberId);
    createPayment.mutate(
      { memberId: paymentForm.memberId, amount, method: paymentForm.method, type: paymentForm.type },
      {
        onSuccess: () => {
          toast.success(`Payment of ${formatCurrency(amount)} recorded for ${member?.name}.`);
          setPaymentForm({ memberId: '', amount: '', type: 'membership', method: 'cash' });
          setShowRecordPayment(false);
        },
        onError: (err) => showApiError(err, 'Failed to record payment.'),
      },
    );
  }

  function openNewPlan() {
    setEditingPlan({ id: '', name: '', description: '', durationDays: 30, price: 0, isActive: true, memberCount: 0 });
    setShowPlanModal(true);
  }
  function openEditPlan(p: MembershipPlan) {
    setEditingPlan({ ...p });
    setShowPlanModal(true);
  }
  function savePlan() {
    if (!editingPlan) return;
    if (!editingPlan.name.trim()) {
      toast.error('Plan name required.');
      return;
    }
    const onSuccess = () => {
      toast.success(editingPlan.id ? 'Plan updated.' : 'Plan created.');
      setShowPlanModal(false);
      setEditingPlan(null);
    };
    const onError = (err: unknown) => showApiError(err, 'Failed to save plan.');
    if (editingPlan.id) {
      updatePlan.mutate(
        { id: editingPlan.id, name: editingPlan.name, durationDays: editingPlan.durationDays, price: editingPlan.price, isActive: editingPlan.isActive },
        { onSuccess, onError },
      );
    } else {
      createPlan.mutate(
        { name: editingPlan.name, durationDays: editingPlan.durationDays, price: editingPlan.price },
        { onSuccess, onError },
      );
    }
  }

  function confirmVoidPayment(p: { id: string; memberName: string; amount: number }) {
    const reason = prompt(`Void payment of ${formatCurrency(p.amount)} for ${p.memberName}?\n\nEnter reason:`);
    if (!reason || !reason.trim()) return;
    voidPayment.mutate(
      { id: p.id, reason: reason.trim() },
      {
        onSuccess: () => toast.success('Payment voided.'),
        onError: (err) => showApiError(err, 'Failed to void payment.'),
      },
    );
  }

  function confirmDeactivatePlan(p: MembershipPlan) {
    if (!confirm(`Deactivate plan "${p.name}"? Existing members keep their plan; new sign-ups won't see it.`)) return;
    updatePlan.mutate(
      { id: p.id, isActive: false },
      {
        onSuccess: () => toast.success('Plan deactivated.'),
        onError: (err) => showApiError(err, 'Failed to deactivate plan.'),
      },
    );
  }

  function confirmDeactivateTemplate(t: SessionPackageTemplate) {
    if (!confirm(`Deactivate package "${t.name}"? Active member packages keep working; new purchases won't see it.`)) return;
    updateTemplate.mutate(
      { id: t.id, isActive: false },
      {
        onSuccess: () => toast.success('Template deactivated.'),
        onError: (err) => showApiError(err, 'Failed to deactivate template.'),
      },
    );
  }

  function openNewTemplate() {
    setEditingTemplate({ id: '', name: '', sessionCount: 10, pricePerSession: 0, totalPrice: 0, validityDays: 90, isActive: true });
    setShowTemplateModal(true);
  }
  function openEditTemplate(t: SessionPackageTemplate) {
    setEditingTemplate({ ...t });
    setShowTemplateModal(true);
  }
  function saveTemplate() {
    if (!editingTemplate) return;
    if (!editingTemplate.name.trim()) {
      toast.error('Template name required.');
      return;
    }
    const onSuccess = () => {
      toast.success(editingTemplate.id ? 'Template updated.' : 'Template created.');
      setShowTemplateModal(false);
      setEditingTemplate(null);
    };
    const onError = (err: unknown) => showApiError(err, 'Failed to save template.');
    if (editingTemplate.id) {
      updateTemplate.mutate(
        { id: editingTemplate.id, name: editingTemplate.name, sessionCount: editingTemplate.sessionCount, pricePerSession: editingTemplate.pricePerSession },
        { onSuccess, onError },
      );
    } else {
      createTemplate.mutate(
        { name: editingTemplate.name, sessionCount: editingTemplate.sessionCount, pricePerSession: editingTemplate.pricePerSession },
        { onSuccess, onError },
      );
    }
  }

  const totalRevenue = recentPayments.filter((p) => p.status === 'completed').reduce((s, p) => s + p.amount, 0);

  return (
    <div className="space-y-6">
      <PageHeader
        title="Billing"
        description="Manage payments, plans, and session packages"
        actions={
          <button
            onClick={() => setShowRecordPayment(true)}
            className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90"
          >
            <Plus className="h-4 w-4" /> Record Payment
          </button>
        }
      />

      {/* KPIs */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <KpiCard value={formatCurrency(totalRevenue)} label="Total Revenue (This Month)" change={8.3} icon={<DollarSign className="h-5 w-5" />} variant="success" />
        <KpiCard value={recentPayments.filter((p) => p.status === 'completed').length.toString()} label="Completed Payments" icon={<CreditCard className="h-5 w-5" />} variant="info" />
        <KpiCard value={recentPayments.filter((p) => p.status === 'voided').length.toString()} label="Voided Payments" icon={<Receipt className="h-5 w-5" />} variant="warning" />
      </div>

      {/* Tabs */}
      <div className="border-b">
        <div className="flex gap-6">
          {tabs.map((tab) => (
            <button key={tab} onClick={() => setActiveTab(tab)} className={`pb-3 text-sm font-medium border-b-2 ${activeTab === tab ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground'}`}>
              {tab}
            </button>
          ))}
        </div>
      </div>

      {activeTab === 'Payments' && (
        <div className="rounded-xl border bg-card shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Member</th>
                <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Type</th>
                <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Method</th>
                <th className="text-right py-2.5 px-4 font-semibold text-muted-foreground">Amount</th>
                <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Status</th>
                <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Date</th>
                <th className="text-right py-2.5 px-4 font-semibold text-muted-foreground">Actions</th>
              </tr>
            </thead>
            <tbody>
              {recentPayments.map((payment) => (
                <tr key={payment.id} className="border-b last:border-0 hover:bg-muted/50">
                  <td className="py-2.5 px-4 font-medium">{payment.memberName}</td>
                  <td className="py-2.5 px-4 capitalize">{payment.type?.replace(/_/g, ' ')}</td>
                  <td className="py-2.5 px-4 capitalize">{payment.method?.replace(/_/g, ' ')}</td>
                  <td className="py-2.5 px-4 text-right font-medium">{formatCurrency(payment.amount)}</td>
                  <td className="py-2.5 px-4"><StatusBadge variant={payment.status} /></td>
                  <td className="py-2.5 px-4">{formatDate(payment.createdAt)}</td>
                  <td className="py-2.5 px-4 text-right">
                    {payment.status === 'completed' && (
                      <button
                        onClick={() => confirmVoidPayment(payment)}
                        disabled={voidPayment.isPending}
                        className="inline-flex items-center gap-1 rounded-md border border-red-200 bg-red-50 px-2 py-1 text-xs text-red-700 hover:bg-red-100 disabled:opacity-50"
                      >
                        <Trash2 className="h-3 w-3" /> Void
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {activeTab === 'Plans' && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-semibold">Membership Plans ({plans.length})</h3>
            <button
              onClick={openNewPlan}
              className="inline-flex items-center gap-2 rounded-lg bg-primary px-3 py-1.5 text-xs font-medium text-white hover:bg-primary/90"
            >
              <Plus className="h-3.5 w-3.5" /> New Plan
            </button>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            {plans.map((p) => (
              <div key={p.id} className="rounded-xl border bg-card p-5 shadow-sm">
                <div className="flex items-start justify-between mb-2">
                  <h4 className="text-base font-semibold">{p.name}</h4>
                  <StatusBadge variant={p.isActive ? 'active' : 'inactive'} label={p.isActive ? 'Active' : 'Inactive'} />
                </div>
                <p className="text-xs text-muted-foreground mb-3">{p.description}</p>
                <div className="mb-3">
                  <div className="text-2xl font-bold">{formatCurrency(p.price)}</div>
                  <div className="text-xs text-muted-foreground">for {p.durationDays} days</div>
                </div>
                <div className="flex items-center gap-1.5 text-xs text-muted-foreground mb-4">
                  <Users className="h-3 w-3" /> {p.memberCount ?? 0} active members
                </div>
                <div className="flex items-center gap-2">
                  <button onClick={() => openEditPlan(p)} className="flex-1 inline-flex items-center justify-center gap-1.5 rounded-md border px-2 py-1.5 text-xs hover:bg-accent">
                    <Edit className="h-3 w-3" /> Edit
                  </button>
                  {p.isActive && (
                    <button
                      onClick={() => confirmDeactivatePlan(p)}
                      disabled={updatePlan.isPending}
                      className="inline-flex items-center justify-center gap-1.5 rounded-md border border-red-200 bg-red-50 px-2 py-1.5 text-xs text-red-700 hover:bg-red-100 disabled:opacity-50"
                    >
                      <Trash2 className="h-3 w-3" /> Deactivate
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {activeTab === 'Packages' && (
        <div className="space-y-6">
          {/* Package Templates */}
          <div>
            <div className="flex items-center justify-between mb-3">
              <h3 className="text-sm font-semibold">Session Package Templates ({templates.length})</h3>
              <button
                onClick={openNewTemplate}
                className="inline-flex items-center gap-2 rounded-lg bg-primary px-3 py-1.5 text-xs font-medium text-white hover:bg-primary/90"
              >
                <Plus className="h-3.5 w-3.5" /> New Template
              </button>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
              {templates.map((t) => (
                <div key={t.id} className="rounded-xl border bg-card p-4 shadow-sm">
                  <div className="flex items-center gap-2 mb-2">
                    <Package className="h-4 w-4 text-primary" />
                    <h4 className="text-sm font-semibold">{t.name}</h4>
                  </div>
                  <div className="grid grid-cols-2 gap-2 text-xs mb-3">
                    <div>
                      <div className="text-muted-foreground">Sessions</div>
                      <div className="font-semibold">{t.sessionCount}</div>
                    </div>
                    <div>
                      <div className="text-muted-foreground">Per session</div>
                      <div className="font-semibold">{formatCurrency(t.pricePerSession)}</div>
                    </div>
                    <div>
                      <div className="text-muted-foreground">Total</div>
                      <div className="font-semibold">{formatCurrency(t.totalPrice)}</div>
                    </div>
                    <div>
                      <div className="text-muted-foreground">Valid</div>
                      <div className="font-semibold">{t.validityDays} days</div>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <button onClick={() => openEditTemplate(t)} className="flex-1 inline-flex items-center justify-center gap-1.5 rounded-md border px-2 py-1.5 text-xs hover:bg-accent">
                      <Edit className="h-3 w-3" /> Edit
                    </button>
                    {t.isActive && (
                      <button
                        onClick={() => confirmDeactivateTemplate(t)}
                        disabled={updateTemplate.isPending}
                        className="inline-flex items-center justify-center gap-1.5 rounded-md border border-red-200 bg-red-50 px-2 py-1.5 text-xs text-red-700 hover:bg-red-100 disabled:opacity-50"
                      >
                        <Trash2 className="h-3 w-3" /> Deactivate
                      </button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Active Packages */}
          <div>
            <div className="flex items-center justify-between mb-3">
              <h3 className="text-sm font-semibold">Active Packages ({activePackages.length})</h3>
              <button
                onClick={() => setShowAssignModal(true)}
                className="inline-flex items-center gap-2 rounded-lg border px-3 py-1.5 text-xs font-medium hover:bg-accent"
              >
                <Plus className="h-3.5 w-3.5" /> Assign to member
              </button>
            </div>
            <div className="rounded-xl border bg-card shadow-sm overflow-hidden">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Member</th>
                    <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Template</th>
                    <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Usage</th>
                    <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Expires</th>
                    <th className="text-left py-2.5 px-4 font-semibold text-muted-foreground">Assigned by</th>
                  </tr>
                </thead>
                <tbody>
                  {activePackages.map((pk) => (
                    <tr key={pk.id} className="border-b last:border-0 hover:bg-muted/50">
                      <td className="py-2.5 px-4 font-medium">{pk.memberName}</td>
                      <td className="py-2.5 px-4">{pk.templateName}</td>
                      <td className="py-2.5 px-4">
                        <div className="flex items-center gap-2">
                          <div className="h-1.5 w-20 rounded-full bg-muted overflow-hidden">
                            <div className="h-full bg-primary" style={{ width: `${(pk.remaining / pk.total) * 100}%` }} />
                          </div>
                          <span className="text-xs text-muted-foreground">{pk.remaining}/{pk.total}</span>
                        </div>
                      </td>
                      <td className="py-2.5 px-4">{formatDate(pk.expiresAt)}</td>
                      <td className="py-2.5 px-4 text-muted-foreground">{pk.assignedBy}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* Plan Edit Modal */}
      <Modal
        open={showPlanModal}
        onClose={() => setShowPlanModal(false)}
        title={editingPlan?.id ? 'Edit Plan' : 'New Plan'}
        size="md"
        footer={
          <>
            <button onClick={() => setShowPlanModal(false)} className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted">Cancel</button>
            <button onClick={savePlan} className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90">Save</button>
          </>
        }
      >
        {editingPlan && (
          <div className="space-y-3">
            <label className="block">
              <span className="text-sm font-medium">Name</span>
              <input value={editingPlan.name} onChange={(e) => setEditingPlan({ ...editingPlan, name: e.target.value })} className="mt-1 w-full h-9 rounded-lg border px-3 text-sm" />
            </label>
            <label className="block">
              <span className="text-sm font-medium">Description</span>
              <input value={editingPlan.description} onChange={(e) => setEditingPlan({ ...editingPlan, description: e.target.value })} className="mt-1 w-full h-9 rounded-lg border px-3 text-sm" />
            </label>
            <div className="grid grid-cols-2 gap-3">
              <label className="block">
                <span className="text-sm font-medium">Duration (days)</span>
                <input type="number" value={editingPlan.durationDays} onChange={(e) => setEditingPlan({ ...editingPlan, durationDays: Number(e.target.value) })} className="mt-1 w-full h-9 rounded-lg border px-3 text-sm" />
              </label>
              <label className="block">
                <span className="text-sm font-medium">Price (EGP)</span>
                <input type="number" value={editingPlan.price} onChange={(e) => setEditingPlan({ ...editingPlan, price: Number(e.target.value) })} className="mt-1 w-full h-9 rounded-lg border px-3 text-sm" />
              </label>
            </div>
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={editingPlan.isActive} onChange={(e) => setEditingPlan({ ...editingPlan, isActive: e.target.checked })} />
              Active
            </label>
          </div>
        )}
      </Modal>

      {/* Template Edit Modal */}
      <Modal
        open={showTemplateModal}
        onClose={() => setShowTemplateModal(false)}
        title={editingTemplate?.id ? 'Edit Template' : 'New Template'}
        size="md"
        footer={
          <>
            <button onClick={() => setShowTemplateModal(false)} className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted">Cancel</button>
            <button onClick={saveTemplate} className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90">Save</button>
          </>
        }
      >
        {editingTemplate && (
          <div className="space-y-3">
            <label className="block">
              <span className="text-sm font-medium">Name</span>
              <input value={editingTemplate.name} onChange={(e) => setEditingTemplate({ ...editingTemplate, name: e.target.value })} className="mt-1 w-full h-9 rounded-lg border px-3 text-sm" />
            </label>
            <div className="grid grid-cols-3 gap-3">
              <label className="block">
                <span className="text-sm font-medium">Sessions</span>
                <input type="number" value={editingTemplate.sessionCount} onChange={(e) => setEditingTemplate({ ...editingTemplate, sessionCount: Number(e.target.value) })} className="mt-1 w-full h-9 rounded-lg border px-3 text-sm" />
              </label>
              <label className="block">
                <span className="text-sm font-medium">Price/session</span>
                <input type="number" value={editingTemplate.pricePerSession} onChange={(e) => setEditingTemplate({ ...editingTemplate, pricePerSession: Number(e.target.value) })} className="mt-1 w-full h-9 rounded-lg border px-3 text-sm" />
              </label>
              <label className="block">
                <span className="text-sm font-medium">Valid (days)</span>
                <input type="number" value={editingTemplate.validityDays} onChange={(e) => setEditingTemplate({ ...editingTemplate, validityDays: Number(e.target.value) })} className="mt-1 w-full h-9 rounded-lg border px-3 text-sm" />
              </label>
            </div>
            <div className="rounded-lg bg-muted px-3 py-2 text-sm">
              Total: <span className="font-semibold">{formatCurrency(editingTemplate.pricePerSession * editingTemplate.sessionCount)}</span>
            </div>
          </div>
        )}
      </Modal>

      {/* Assign Package Modal */}
      <Modal
        open={showAssignModal}
        onClose={() => setShowAssignModal(false)}
        title="Assign Package to Member"
        size="md"
        footer={
          <>
            <button onClick={() => setShowAssignModal(false)} className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted">Cancel</button>
            <button onClick={submitAssign} className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90">Assign</button>
          </>
        }
      >
        <div className="space-y-3">
          <label className="block">
            <span className="text-sm font-medium">Member</span>
            <select value={assignForm.memberId} onChange={(e) => setAssignForm({ ...assignForm, memberId: e.target.value })} className="mt-1 w-full h-9 rounded-lg border px-3 text-sm">
              <option value="">Select member...</option>
              {members.map((m) => <option key={m.id} value={m.id}>{m.name}</option>)}
            </select>
          </label>
          <label className="block">
            <span className="text-sm font-medium">Template</span>
            <select value={assignForm.templateId} onChange={(e) => setAssignForm({ ...assignForm, templateId: e.target.value })} className="mt-1 w-full h-9 rounded-lg border px-3 text-sm">
              <option value="">Select template...</option>
              {templates.filter((t) => t.isActive).map((t) => <option key={t.id} value={t.id}>{t.name} — {formatCurrency(t.totalPrice)}</option>)}
            </select>
          </label>
        </div>
      </Modal>

      {/* Record Payment Modal */}
      <Modal
        open={showRecordPayment}
        onClose={() => setShowRecordPayment(false)}
        title="Record Payment"
        size="md"
        footer={
          <>
            <button onClick={() => setShowRecordPayment(false)} className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted">Cancel</button>
            <button onClick={submitPayment} className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90">Record Payment</button>
          </>
        }
      >
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1.5">Member</label>
            <select value={paymentForm.memberId} onChange={(e) => setPaymentForm({ ...paymentForm, memberId: e.target.value })} className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm">
              <option value="">Select member...</option>
              {members.map((m) => <option key={m.id} value={m.id}>{m.name}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Amount (EGP)</label>
            <input type="number" value={paymentForm.amount} onChange={(e) => setPaymentForm({ ...paymentForm, amount: e.target.value })} className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm" />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Payment Type</label>
            <select value={paymentForm.type} onChange={(e) => setPaymentForm({ ...paymentForm, type: e.target.value })} className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm">
              <option value="membership">Membership</option>
              <option value="session_package">Session Package</option>
              <option value="personal_training">Personal Training</option>
              <option value="class">Class</option>
              <option value="other">Other</option>
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Payment Method</label>
            <select value={paymentForm.method} onChange={(e) => setPaymentForm({ ...paymentForm, method: e.target.value })} className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm">
              <option value="cash">Cash</option>
              <option value="card">Card</option>
              <option value="vodafone_cash">Vodafone Cash</option>
              <option value="instapay">InstaPay</option>
              <option value="fawry">Fawry</option>
              <option value="bank_transfer">Bank Transfer</option>
            </select>
          </div>
        </div>
      </Modal>
    </div>
  );
}

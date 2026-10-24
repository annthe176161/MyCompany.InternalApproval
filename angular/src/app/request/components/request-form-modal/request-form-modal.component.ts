import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd/message';
import {
  RequestService,
  RequestDto,
  RequestType,
  CreateRequestDto,
  UpdateRequestDto,
} from '../../../proxy/requests';
import {
  calculateLeaveDays,
  extractErrorMessage,
  formatCurrencyInput,
  formatDateVn,
  formatToLocalIso,
  parseCurrencyInput,
} from '../../../shared/utils';

// Modal tạo mới hoặc chỉnh sửa thông tin đề xuất
@Component({
  selector: 'app-request-form-modal',
  templateUrl: './request-form-modal.component.html',
  styleUrls: ['./request-form-modal.component.scss'],
})
export class RequestFormModalComponent implements OnInit {
  @Output() saveSuccess = new EventEmitter<void>();

  RequestType = RequestType;
  isVisible = false;
  isSaving = false;
  isEditing = false;
  currentRequestId: string | null = null;
  form!: FormGroup;

  currencyFormatter = formatCurrencyInput;
  currencyParser = parseCurrencyInput;

  constructor(
    private fb: FormBuilder,
    private requestService: RequestService,
    private message: NzMessageService,
  ) {}

  ngOnInit(): void {
    this.initForm();
  }

  private initForm(): void {
    this.form = this.fb.group({
      type: [RequestType.Leave, [Validators.required]],
      title: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', [Validators.maxLength(4000)]],
      amount: [{ value: null, disabled: true }],
      startDate: [null],
      endDate: [null],
    });

    this.form.get('type')?.valueChanges.subscribe((type: RequestType) => {
      this.handleTypeChange(type);
    });
  }

  get calculatedLeaveDays(): number {
    const start = this.form?.get('startDate')?.value;
    const end = this.form?.get('endDate')?.value;
    return calculateLeaveDays(start, end) ?? 1;
  }

  disabledEndDate = (endValue: Date): boolean => {
    const start = this.form?.get('startDate')?.value;
    if (!endValue || !start) return false;
    const s = new Date(start);
    s.setHours(0, 0, 0, 0);
    const e = new Date(endValue);
    e.setHours(0, 0, 0, 0);
    return e.getTime() < s.getTime();
  };

  onStartDateChange(date: Date | null): void {
    if (!date) return;
    const endDate = this.form.get('endDate')?.value;
    if (!endDate || new Date(endDate).getTime() < new Date(date).getTime()) {
      this.form.get('endDate')?.setValue(date);
    }
    this.updateLeaveTitleSuggestion();
  }

  onEndDateChange(_date: Date | null): void {
    this.updateLeaveTitleSuggestion();
  }

  // Tự động gợi ý tiêu đề đơn xin nghỉ phép theo khoảng ngày đã chọn
  private updateLeaveTitleSuggestion(): void {
    const type = this.form.get('type')?.value;
    if (type !== RequestType.Leave) return;

    const start = this.form.get('startDate')?.value;
    const end = this.form.get('endDate')?.value;
    if (!start) return;

    const currentTitle = (this.form.get('title')?.value || '').trim();
    if (!currentTitle || currentTitle.startsWith('Đơn xin nghỉ phép')) {
      const days = this.calculatedLeaveDays;
      const startStr = formatDateVn(start);
      const endStr = end ? formatDateVn(end) : startStr;
      const periodStr = startStr === endStr ? startStr : `từ ${startStr} đến ${endStr}`;
      this.form.get('title')?.setValue(`Đơn xin nghỉ phép (${days} ngày: ${periodStr})`);
    }
  }

  private handleTypeChange(type: RequestType): void {
    const amountCtrl = this.form.get('amount');
    const startDateCtrl = this.form.get('startDate');
    const endDateCtrl = this.form.get('endDate');

    if (type === RequestType.Equipment || type === RequestType.AdvanceMoney) {
      amountCtrl?.enable();
      amountCtrl?.setValidators([Validators.required, Validators.min(1000)]);

      startDateCtrl?.clearValidators();
      startDateCtrl?.setValue(null);

      endDateCtrl?.clearValidators();
      endDateCtrl?.setValue(null);

      const currentTitle = this.form.get('title')?.value || '';
      if (currentTitle.startsWith('Đơn xin nghỉ phép')) {
        this.form.get('title')?.setValue('');
      }
    } else {
      // Trường hợp loại đề xuất là Nghỉ phép (Leave)
      amountCtrl?.disable();
      amountCtrl?.clearValidators();
      amountCtrl?.setValue(null);

      startDateCtrl?.setValidators([Validators.required]);
      endDateCtrl?.setValidators([Validators.required]);

      if (!startDateCtrl?.value) {
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        startDateCtrl?.setValue(today);
        endDateCtrl?.setValue(today);
      }
      this.updateLeaveTitleSuggestion();
    }

    amountCtrl?.updateValueAndValidity();
    startDateCtrl?.updateValueAndValidity();
    endDateCtrl?.updateValueAndValidity();
  }

  openCreate(): void {
    this.isEditing = false;
    this.currentRequestId = null;
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    this.form.reset({
      type: RequestType.Leave,
      title: '',
      description: '',
      amount: null,
      startDate: today,
      endDate: today,
    });

    this.handleTypeChange(RequestType.Leave);
    this.updateLeaveTitleSuggestion();
    this.isVisible = true;
  }

  openEdit(req: RequestDto): void {
    this.isEditing = true;
    this.currentRequestId = req.id;
    const start = req.startDate ? new Date(req.startDate) : null;
    const end = req.endDate ? new Date(req.endDate) : start ? new Date(start) : null;

    this.form.patchValue({
      type: req.type,
      title: req.title,
      description: req.description,
      amount: req.amount,
      startDate: start,
      endDate: end,
    });

    this.handleTypeChange(req.type);
    this.isVisible = true;
  }

  close(): void {
    this.isVisible = false;
  }

  save(andSubmit: boolean = false): void {
    if (this.form.invalid) {
      Object.values(this.form.controls).forEach(control => {
        if (control.invalid) {
          control.markAsDirty();
          control.updateValueAndValidity({ onlySelf: true });
        }
      });
      return;
    }

    this.isSaving = true;
    const formVal = this.form.getRawValue();

    let startIso: string | undefined = undefined;
    let endIso: string | undefined = undefined;

    if (formVal.type === RequestType.Leave) {
      if (formVal.startDate) {
        startIso = formatToLocalIso(formVal.startDate);
      }
      if (formVal.endDate) {
        endIso = formatToLocalIso(formVal.endDate);
      }
    }

    const payload: CreateRequestDto = {
      title: formVal.title,
      type: formVal.type,
      description: formVal.description,
      amount: formVal.type !== RequestType.Leave ? formVal.amount : undefined,
      startDate: startIso,
      endDate: endIso,
    };

    if (this.isEditing && this.currentRequestId) {
      this.requestService
        .update(this.currentRequestId, payload as UpdateRequestDto)
        .subscribe({
          next: () => {
            this.message.success('Cập nhật đề xuất thành công!');
            this.isSaving = false;
            this.isVisible = false;
            this.saveSuccess.emit();
          },
          error: err => {
            this.message.error(extractErrorMessage(err, 'Không thể cập nhật đề xuất!'));
            this.isSaving = false;
          },
        });
    } else {
      this.requestService.create(payload).subscribe({
        next: created => {
          if (andSubmit) {
            this.requestService.submit(created.id).subscribe({
              next: () => {
                this.message.success('Tạo và gửi phê duyệt thành công!');
                this.isSaving = false;
                this.isVisible = false;
                this.saveSuccess.emit();
              },
              error: err => {
                this.message.warning(
                  extractErrorMessage(err, 'Đã tạo đề xuất nhưng không thể gửi phê duyệt!'),
                );
                this.isSaving = false;
                this.isVisible = false;
                this.saveSuccess.emit();
              },
            });
          } else {
            this.message.success('Lưu nháp đề xuất thành công!');
            this.isSaving = false;
            this.isVisible = false;
            this.saveSuccess.emit();
          }
        },
        error: err => {
          this.message.error(extractErrorMessage(err, 'Không thể tạo đề xuất!'));
          this.isSaving = false;
        },
      });
    }
  }
}

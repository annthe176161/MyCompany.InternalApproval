import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd/message';
import { RequestService, RequestDto, RequestType } from '../../../proxy/requests';

@Component({
  selector: 'app-request-form-modal',
  templateUrl: './request-form-modal.component.html',
})
export class RequestFormModalComponent implements OnInit {
  @Output() saveSuccess = new EventEmitter<void>();

  RequestType = RequestType;
  isVisible = false;
  isSaving = false;
  isEditing = false;
  currentRequestId: string | null = null;
  form!: FormGroup;

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
    });

    this.form.get('type')?.valueChanges.subscribe((type: RequestType) => {
      const amountCtrl = this.form.get('amount');
      if (type === RequestType.Equipment || type === RequestType.AdvanceMoney) {
        amountCtrl?.enable();
        amountCtrl?.setValidators([Validators.required, Validators.min(1000)]);
      } else {
        amountCtrl?.disable();
        amountCtrl?.clearValidators();
        amountCtrl?.setValue(null);
      }
      amountCtrl?.updateValueAndValidity();
    });
  }

  openCreate(): void {
    this.isEditing = false;
    this.currentRequestId = null;
    this.form.reset({
      type: RequestType.Leave,
      title: '',
      description: '',
      amount: null,
    });
    this.isVisible = true;
  }

  openEdit(req: RequestDto): void {
    this.isEditing = true;
    this.currentRequestId = req.id;
    this.form.patchValue({
      type: req.type,
      title: req.title,
      description: req.description,
      amount: req.amount,
    });

    const amountCtrl = this.form.get('amount');
    if (req.type === RequestType.Equipment || req.type === RequestType.AdvanceMoney) {
      amountCtrl?.enable();
      amountCtrl?.setValidators([Validators.required, Validators.min(1000)]);
    } else {
      amountCtrl?.disable();
      amountCtrl?.clearValidators();
    }
    amountCtrl?.updateValueAndValidity();

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

    if (this.isEditing && this.currentRequestId) {
      this.requestService
        .update(this.currentRequestId, {
          title: formVal.title,
          type: formVal.type,
          description: formVal.description,
          amount: formVal.amount,
        })
        .subscribe({
          next: () => {
            this.message.success('Cập nhật đề xuất thành công!');
            this.isSaving = false;
            this.isVisible = false;
            this.saveSuccess.emit();
          },
          error: err => {
            const msg = this.extractErrorMessage(err, 'Không thể cập nhật đề xuất!');
            this.message.error(msg);
            this.isSaving = false;
          },
        });
    } else {
      this.requestService
        .create({
          title: formVal.title,
          type: formVal.type,
          description: formVal.description,
          amount: formVal.amount,
        })
        .subscribe({
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
                  const msg = this.extractErrorMessage(
                    err,
                    'Đã tạo đề xuất nhưng không thể gửi phê duyệt!'
                  );
                  this.message.warning(msg);
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
            const msg = this.extractErrorMessage(err, 'Không thể tạo đề xuất!');
            this.message.error(msg);
            this.isSaving = false;
          },
        });
    }
  }

  private extractErrorMessage(err: any, fallback: string): string {
    const validationErrors = err.error?.error?.validationErrors;
    if (Array.isArray(validationErrors) && validationErrors.length > 0) {
      return validationErrors.map((v: any) => v.message).filter(Boolean).join('; ');
    }
    return err.error?.error?.message || fallback;
  }

  currencyFormatter = (value: number): string => {
    return value ? `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',') : '';
  };

  currencyParser = (value: string): string => {
    return value.replace(/\$\s?|(,*)/g, '');
  };
}

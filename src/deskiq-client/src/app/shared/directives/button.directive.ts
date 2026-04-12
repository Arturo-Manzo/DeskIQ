import { Directive, ElementRef, HostBinding, Input, OnInit, inject } from '@angular/core';

type ButtonVariant = 'primary' | 'secondary' | 'neutral' | 'info' | 'warning' | 'danger';
type ButtonSize = 'sm' | 'md' | 'lg';

@Directive({
  selector: 'button:not([fd-button])',
  standalone: true,
})
export class ButtonDirective implements OnInit {
  private readonly elementRef = inject(ElementRef<HTMLButtonElement>);

  private explicitVariant: ButtonVariant | null = null;
  private explicitSize: ButtonSize | null = null;
  private inferredVariant: ButtonVariant = 'secondary';
  private inferredSize: ButtonSize = 'md';

  @Input('appButton')
  set appButton(value: ButtonVariant | null | undefined) {
    this.explicitVariant = this.isVariant(value) ? value : null;
  }

  @Input()
  set buttonSize(value: ButtonSize | null | undefined) {
    this.explicitSize = this.isSize(value) ? value : null;
  }

  @HostBinding('class.ui-btn')
  readonly uiButtonClass = true;

  @HostBinding('attr.data-btn-variant')
  get variantAttr(): ButtonVariant {
    return this.explicitVariant ?? this.inferredVariant;
  }

  @HostBinding('attr.data-btn-size')
  get sizeAttr(): ButtonSize {
    return this.explicitSize ?? this.inferredSize;
  }

  ngOnInit(): void {
    const classes = this.elementRef.nativeElement.classList;

    this.inferredVariant = this.detectVariant(classes, this.elementRef.nativeElement.type);
    this.inferredSize = this.detectSize(classes);
  }

  private detectVariant(classes: DOMTokenList, type: string): ButtonVariant {
    if (classes.contains('btn-success')) {
      return 'primary';
    }

    if (classes.contains('btn-warning')) {
      return 'warning';
    }

    if (classes.contains('btn-danger')) {
      return 'danger';
    }

    if (classes.contains('btn-info')) {
      return 'info';
    }

    if (classes.contains('btn-neutral')) {
      return 'neutral';
    }

    return type === 'submit' ? 'primary' : 'secondary';
  }

  private detectSize(classes: DOMTokenList): ButtonSize {
    if (classes.contains('text-xs') || classes.contains('rounded-md')) {
      return 'sm';
    }

    if (classes.contains('px-6') || classes.contains('py-3') || classes.contains('rounded-xl')) {
      return 'lg';
    }

    return 'md';
  }

  private isVariant(value: unknown): value is ButtonVariant {
    return value === 'primary' || value === 'secondary' || value === 'neutral' || value === 'info' || value === 'warning' || value === 'danger';
  }

  private isSize(value: unknown): value is ButtonSize {
    return value === 'sm' || value === 'md' || value === 'lg';
  }
}

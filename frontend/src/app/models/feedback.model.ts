export enum FeedbackKind {
  Success = 'success',
  Error = 'error'
}

export interface Feedback {
  kind: FeedbackKind;
  message: string;
}

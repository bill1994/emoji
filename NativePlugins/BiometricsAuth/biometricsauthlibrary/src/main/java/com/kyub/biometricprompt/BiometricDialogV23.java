package com.kyub.biometricprompt;

import com.kyub.biometricauthlibrary.R;
import android.app.Activity;
import android.content.Context;
import android.content.ContextWrapper;
import android.graphics.drawable.Drawable;
import android.view.View;
import android.widget.Button;
import android.widget.ImageView;
import android.widget.TextView;

import com.google.android.material.bottomsheet.BottomSheetDialog;

import androidx.annotation.NonNull;

public class BiometricDialogV23 extends BottomSheetDialog implements View.OnClickListener {

    private Activity activity;
    private Context context;

    private Button btnCancel;
    private ImageView imgLogo;
    private TextView itemTitle, itemDescription, itemSubtitle, itemStatus;

    private BiometricCallback biometricCallback;

    public BiometricDialogV23(@NonNull Context context) {
        super(context, R.style.BottomSheetDialogTheme);
        this.activity = getActivity(context);
        this.context = context.getApplicationContext();
        setDialogView();
    }

    public BiometricDialogV23(@NonNull Context context, BiometricCallback biometricCallback) {
        super(context, R.style.BottomSheetDialogTheme);
        this.activity = getActivity(context);
        this.context = context.getApplicationContext();
        this.biometricCallback = biometricCallback;
        setDialogView();
    }

    public BiometricDialogV23(@NonNull Context context, int theme) {
        super(context, theme);
    }

    protected BiometricDialogV23(@NonNull Context context, boolean cancelable, OnCancelListener cancelListener) {
        super(context, cancelable, cancelListener);
    }

    private void setDialogView() {
        View bottomSheetView = getLayoutInflater().inflate(R.layout.view_bottom_sheet, null);
        setContentView(bottomSheetView);

        btnCancel = findViewById(R.id.btn_cancel);
        btnCancel.setOnClickListener(this);

        imgLogo = findViewById(R.id.img_logo);
        itemTitle = findViewById(R.id.item_title);
        itemStatus = findViewById(R.id.item_status);
        itemSubtitle = null; //findViewById(R.id.item_subtitle);
        itemDescription = findViewById(R.id.item_description);

        updateLogo();
    }

    public void setTitle(String title) {
        final String runnableTitle = title;

        safeRunOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    if (itemTitle != null)
                        itemTitle.setText(runnableTitle != null ? runnableTitle : "");
                } catch (Exception e) {
                    e.printStackTrace();
                }
            }
        });
    }

    public void updateStatus(String status) {
        final String runnableStatus = status;

        safeRunOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    if (itemStatus != null)
                        itemStatus.setText(runnableStatus != null ? runnableStatus : "");
                } catch (Exception e) {
                    e.printStackTrace();
                }
            }
        });
    }

    public void setSubtitle(String subtitle) {
        final String runnableSubtitle = subtitle;

        safeRunOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    if (itemSubtitle != null)
                        itemSubtitle.setText(runnableSubtitle != null ? runnableSubtitle : "");
                } catch (Exception e) {
                    e.printStackTrace();
                }
            }
        });
    }

    public void setDescription(String description) {
        final String runnableDescription = description;

        safeRunOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    if (itemDescription != null)
                        itemDescription.setText(runnableDescription != null ? runnableDescription : "");
                } catch (Exception e) {
                    e.printStackTrace();
                }
            }
        });
    }

    public void setButtonText(String negativeButtonText) {
        final String runnableNegativeButtonText = negativeButtonText;

        safeRunOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    if (btnCancel != null)
                        btnCancel.setText(runnableNegativeButtonText != null ? runnableNegativeButtonText : "");
                } catch (Exception e) {
                    e.printStackTrace();
                }
            }
        });
    }

    private void updateLogo() {
        safeRunOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    if (imgLogo != null) {
                        Drawable drawable = getContext().getPackageManager().getApplicationIcon(context.getPackageName());
                        imgLogo.setImageDrawable(drawable);
                    }
                } catch (Exception e) {
                    e.printStackTrace();
                }
            }
        });
    }

    public void safeRunOnUiThread(Runnable runnable) {

        if (this.activity != null) {
            try {
                this.activity.runOnUiThread(runnable);
            } catch (Exception e) {
                e.printStackTrace();
            }
        }
    }

    public Activity getActivity(Context context)
    {
        if (context == null)
        {
            return null;
        }
        else if (context instanceof ContextWrapper)
        {
            if (context instanceof Activity)
            {
                return (Activity) context;
            }
            else
            {
                Activity activity = getActivity(((ContextWrapper) context).getBaseContext());
                return activity;
            }
        }

        return null;
    }

    @Override
    public void onClick(View view) {
        dismiss();
        biometricCallback.onAuthenticationCancelled();
    }
}
